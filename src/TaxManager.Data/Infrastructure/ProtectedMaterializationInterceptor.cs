using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Infrastructure;

internal sealed class ProtectedMaterializationInterceptor : IMaterializationInterceptor
{
    internal static readonly ProtectedMaterializationInterceptor Instance = new();
    public object InitializedInstance(MaterializationInterceptionData data, object entity)
    {
        ((TaxManagerDbContext)data.Context).BindProtectedEntity(entity);
        return entity;
    }
}
