using Rhythm.Contracts.Services;

namespace Rhythm.Services;

public class SupabaseConnectionException : Exception
{

    public SupabaseConnectionException(string message) : base(message)
    {

    }
}

public class StorageService : IStorageService
{
    private readonly string _supabaseUrl = "SUPABASE_URL";
    private readonly string _supabaseKey = "SUPABASE_KEY";
    private bool _connected = false;

    public Supabase.Client? Client
    {
        get;
        private set;
    }

    public StorageService()
    {
        _ = ConnectToSupabase();
    }

    public async Task<bool> ConnectToSupabase()
    {
        if (_connected) return true;
        try
        {
            Client = new Supabase.Client(_supabaseUrl, _supabaseKey);
            await Client.InitializeAsync();
            _connected = true;
            return true;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
            return false;
        }
    }

    public void DisconnectFromSupabase()
    {
        if (_connected)
        {
            Client = null;
            _connected = false;
        }
    }

    public Supabase.Client GetClient()
    {

        if (_connected)
        {
            return Client!;
        }
        else
        {
            throw new SupabaseConnectionException("Not connected to Supabase.");
        }
    }

    public async Task<string> UploadAvatar(byte[] image, string name)
    {
        if (!_connected) throw new SupabaseConnectionException("Not connected to Supabase.");
        var storage = Client!.Storage;
        var bucket = storage.From("Avatars");
        await bucket.Upload(image, name, new Supabase.Storage.FileOptions
        {
            Upsert = true
        });
        return $"{bucket.GetPublicUrl(name)}?t={DateTime.Now.Ticks}";
    }

    public async Task DeleteAvatar(string name)
    {
        if (!_connected) throw new SupabaseConnectionException("Not connected to Supabase.");
        var storage = Client!.Storage;
        var bucket = storage.From("Avatars");
        await bucket.Remove(name);
    }
}
