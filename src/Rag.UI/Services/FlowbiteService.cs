using Microsoft.JSInterop;

namespace Rag.UI.Services;

public interface IFlowbiteService
{
    ValueTask InitializeFlowbiteAsync();
}

public class FlowbiteService : IFlowbiteService
{
    private readonly IJSRuntime _jsRuntime;

    public FlowbiteService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async ValueTask InitializeFlowbiteAsync()
    {
        await _jsRuntime.InvokeVoidAsync("flowbiteInterop.initializeFlowbite");
        // Reapply theme after Flowbite init (defensive)
        await _jsRuntime.InvokeVoidAsync("themeToggler.applySavedTheme");
    }
}