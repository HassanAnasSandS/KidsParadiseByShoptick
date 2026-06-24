namespace KidsParadiseByShoptick.AdminApp.Helpers;

public sealed class PagedListLoadCoordinator
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _currentPage = 1;

    public int CurrentPage => _currentPage;

    public bool SuppressFilterReload { get; private set; } = true;

    public void SuppressFilters(bool suppress = true) => SuppressFilterReload = suppress;

    public void BeginReload()
    {
        _currentPage = 1;
        SuppressFilterReload = true;
    }

    public bool CompletePage(int itemsCount, int totalCount)
    {
        var hasMore = itemsCount < totalCount;
        if (hasMore)
            _currentPage++;
        SuppressFilterReload = false;
        return hasMore;
    }

    public async Task RunExclusiveAsync(Func<Task> action)
    {
        await _gate.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    public static bool CanReload(bool isBusy, bool isLoadingMore) => !isBusy && !isLoadingMore;

    public static bool CanLoadMore(bool hasMore, bool isBusy, bool isLoadingMore, int itemCount) =>
        hasMore && !isBusy && !isLoadingMore && itemCount > 0;
}
