namespace JobHunt.Api.Features.Pii;

public interface IPiiMappingStore
{
    void Put(PiiMapping mapping);

    IReadOnlyList<PiiMapping> GetAll();
}