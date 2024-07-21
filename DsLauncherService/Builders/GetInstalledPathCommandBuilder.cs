using DibBase.Infrastructure;
using DsLauncherService.Args;
using DsLauncherService.Communication;
using DsLauncherService.Storage;

namespace DsLauncherService.Builders;

class GetInstalledPathCommandBuilder(Repository<Installed> repo) : ICommandBuilder
{
    public string Name => "get-installed-path";

    public async Task<Response> Build(Guid productGuid, CancellationToken ct)
    {
        var product = (await repo.GetAll(restrict: x => x.ProductGuid == productGuid, expand: [x => x.Library], ct: ct)).FirstOrDefault()
            ?? throw new($"No product {productGuid} among installed products");

        var path = Path.Combine(product.Library!.Path, productGuid.ToString());
        return new Response(Name, new GetInstalledPathCommandArgs { Path = path });
    }
}