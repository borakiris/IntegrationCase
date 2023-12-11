using Integration.Common;
using Integration.Backend;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    private object m_LockObject = new object();
    private static Dictionary<String, Object> LockList = new Dictionary<string, object>();

    public static int jobCounter;
    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        try
        {
            LockOnValue(itemContent);
            Interlocked.Increment(ref jobCounter);
            // Check the backend to see if the content is already saved.
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
        finally
        {
            UnlockOnValue(itemContent);
            Interlocked.Decrement(ref jobCounter);
        }

    }
    private void LockOnValue(String content)
    {
        lock (LockList.Keys)
        {
            if (!LockList.Keys.Contains(content))
            {
                LockList.Add(content, new Object());
            }
            System.Threading.Monitor.Enter(LockList[content]);
        }
    }

    private void UnlockOnValue(String content)
    {
        System.Threading.Monitor.Exit(LockList[content]);
    }
    public List<Item> GetAllItems()
    {
        while (ItemIntegrationService.jobCounter != 0)
        {
            Thread.Sleep(200);
        }
        return ItemIntegrationBackend.GetAllItems();
    }
}