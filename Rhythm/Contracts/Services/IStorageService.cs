namespace Rhythm.Contracts.Services;
internal interface IStorageService
{
    Supabase.Client? Client
    {
        get;
    }

    Task<bool> ConnectToSupabase();

    Task<string> UploadAvatar(byte[] image, string name);

    Task DeleteAvatar(string name);

    void DisconnectFromSupabase();

    Supabase.Client GetClient();
}
