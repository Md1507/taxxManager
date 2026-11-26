using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Forecasting;

public sealed class ForecastStoreTests
{
    [Fact]
    public async Task Missing_sentinel_is_not_reinitialized_when_forecast_data_remains()
    {
        using var db = new SqliteFileDatabase();
        await using (var setup = db.Open())
        {
            await setup.Database.MigrateAsync();
            await new ForecastStore(setup).CreateAsync("{\"note\":\"original\"}");
            await setup.Database.ExecuteSqlRawAsync("DELETE FROM AuditEvents; DELETE FROM DataProtectionMetadata;");
        }
        await using var reopened = db.Open();
        await Assert.ThrowsAsync<DataProtectionException>(() => new ForecastStore(reopened).CreateAsync("{\"note\":\"new\"}"));
        Assert.Equal(0, await reopened.Set<DataProtectionMetadata>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Updating_preserves_the_original_encrypted_snapshot()
    {
        using var db = new SqliteFileDatabase();
        Guid id;
        await using (var setup = db.Open())
        {
            await setup.Database.MigrateAsync();
            id = await new ForecastStore(setup).CreateAsync("{\"note\":\"lúc đầu\"}");
        }
        await using (var edit = db.Open())
            await new ForecastStore(edit).UpdateAsync(id, 1, "{\"note\":\"đã sửa\"}");
        await using var reopened = db.Open();
        var versions = await new ForecastStore(reopened).HistoryAsync(id);
        Assert.Equal(new[] { "{\"note\":\"lúc đầu\"}", "{\"note\":\"đã sửa\"}" }, versions.Select(v => v.Payload));
    }

    [Fact]
    public async Task Stale_update_does_not_overwrite_a_newer_document()
    {
        using var db = new SqliteFileDatabase();
        Guid id;
        await using (var setup = db.Open())
        {
            await setup.Database.MigrateAsync();
            id = await new ForecastStore(setup).CreateAsync("{\"note\":\"old\"}");
        }
        await using (var newer = db.Open())
            await new ForecastStore(newer).UpdateAsync(id, 1, "{\"note\":\"new\"}");
        await using (var stale = db.Open())
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ForecastStore(stale).UpdateAsync(id, 1, "{\"note\":\"stale\"}"));
        await using var reopened = db.Open();
        var actual = await new ForecastStore(reopened).ReadAsync(id);
        Assert.Equal("{\"note\":\"new\"}", actual!.Payload);
        Assert.Equal(2, actual.Revision);
    }

    [Fact]
    public async Task New_context_reopens_the_exact_saved_document()
    {
        using var db = new SqliteFileDatabase();
        Guid id;
        const string input = "{\"gross\":12345678.123456789,\"insurance\":0,\"withheld\":null,\"note\":\"ghi chú giả định\"}";
        await using (var context = db.Open())
        {
            await context.Database.MigrateAsync();
            id = await new ForecastStore(context).CreateAsync(input);
        }
        await using (var reopened = db.Open())
        {
            var document = await new ForecastStore(reopened).ReadAsync(id);
            Assert.NotNull(document);
            Assert.Equal(input, document.Payload);
            Assert.Equal(1, document.Revision);
        }
    }
}
