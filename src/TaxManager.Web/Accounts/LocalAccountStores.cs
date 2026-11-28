using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Accounts;

public sealed class LocalAccountStores : IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<SyntheticDatabase>> stores = new();
    public string IdentityConnection { get; }

    public LocalAccountStores(SyntheticDatabase original)
    {
        var directory = Path.Combine(original.RootPath, "accounts");
        var existed = Directory.Exists(directory);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "identity.db");
        foreach (var item in new[] { original.RootPath, directory, path, path + "-wal", path + "-shm", path + "-journal" })
            if (Path.Exists(item) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Registry synthetic không được chứa liên kết.");
        if (existed && !File.Exists(path))
            throw new InvalidOperationException("Registry tài khoản bị thiếu; không tạo registry thay thế.");
        IdentityConnection = new SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true, Pooling = false }.ToString();
    }

    public SyntheticDatabase Get(string userId, string root)
    {
        if (!Guid.TryParse(userId, out _) || string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("Tài khoản không có kho hợp lệ.");
        var store = stores.GetOrAdd(userId, _ => new(() => new SyntheticDatabase(Directory.GetCurrentDirectory(), root))).Value;
        if (!string.Equals(store.RootPath, root, StringComparison.Ordinal))
            throw new InvalidOperationException("Kho tài khoản đã thay đổi. Cần khởi động lại.");
        return store;
    }

    public void Dispose()
    {
        foreach (var value in stores.Values)
            if (value.IsValueCreated) value.Value.Dispose();
    }
}
