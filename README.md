<p align="center"><img src="./Rhythm/Assets/StoreLogo.scale-400.png" alt="Logo" width="248" height="248"></p>
<h1 align="center">RHYTHM</h1>
<h4 align="center">A simple music player app for Windows 10 and above using WinUI3</h4>

# Features

- [x] Built using WinUI3, sports modern fluent design.
- [x] Windows OS responsive theme and accent color support.
- [x] Play, Pause, Next, Previous, Shuffle, Repeat, Seek, Volume controls.
- [x] Realtime music streaming
- [x] Customized recommendations based on user's listening history.
- [x] User authentication and profile management.
- [x] User can create and manage playlists.
- [x] User can like, follow and save albums, artists and songs.

# Screenshots

| Main Page | Themes |
| --- | --- |
| ![Main Page](./Assets/main-page.png) | ![Themes](./Assets/themes.png) |
| Playlist Page | Search Page |
| ![Playlist Page](./Assets/playlist.png) | ![Search Page](./Assets/search.png) |
| Album Page | Artist Page |
| ![Album Page](./Assets/album.png) | ![Artist Page](./Assets/artist.png) |

# Demo

Base WinUi App Setup.


https://github.com/FallenDeity/Rhythm/assets/61227305/a4398082-a7ed-4832-a26d-325bc9a6672c


![Demo](Assets/demo.png)

## Setup Repository

You need to add a secret to your github repository to get the workflow actions up and working. Follow the given steps to do so:

- Right click on your solution folder and click `Package and Publish` > `Create App Packages...`

![Package](Assets/package.png)

- For now during development we will use `sideloading` to distribute artifacts from builds and releases.

![SideLoad](Assets/sideload.png)

> [!NOTE]
> `SideLoading` here enables us to install the app on our local machine without the need of a store. This is useful for development and testing purposes.

- Click `Create` and then `Yes, select a certificate` and then `Create` again. A pop-up will appear asking for a password. Leave it empty and click `OK`.

![Create](Assets/create.png)

- Once the process is complete, It will show the following screen and a file named `<ProjectName>_TemporaryKey.pfx` will be created in your solution folder. Once you have the file, you can close the window by canceling the process.

![Cancel](Assets/cancel.png)

- Now, you need to get a `Base64` encoded string of the certificate file. You can use the following command in windows powershell to get the `Base64` encoded string of the certificate file.

```powershell
$cert = Get-Content -Path "<ProjectName>_TemporaryKey.pfx" -Encoding Byte
[System.Convert]::ToBase64String($cert) | Out-File -FilePath "<ProjectName>_TemporaryKey.txt"
```

![Pfx](Assets/pfx.png)

> [!NOTE]
> Replace `<ProjectName>` with your project name. The above command will create a file named `<ProjectName>_TemporaryKey.txt` in your solution folder.

- Now that you have the `Base64` encoded string of the certificate file, you need to add it as a secret to your github repository. Follow the given steps to do so:

  - Go to your repository on github and click on `Settings` > `Secrets` > `New repository secret`.
  - Add a secret with the name `BASE64_ENCODED_PFX` and paste the `Base64` encoded string of the certificate file in the value field.
  - Click `Add secret` to save the secret.
  - Now, the secret is added to your repository and the workflow actions will be able to use it.

![Secrets](Assets/secret.png)

Once you have completed all the above steps, you can now delete the certificate file and the `Base64` encoded string of the certificate file from your solution folder. Do not commit the certificate file or the `Base64` encoded string of the certificate file to your repository.

## Installation Instructions

On a successful build, the artifacts will be available in the workflow actions. You can download the artifacts and install the app on your local machine using the following steps:

Go to your repository on github and click on `Actions` > `Latest workflow run`.

![Artifact](Assets/artifact.png)

Install the artifact based on your architecture. For example, if you are using a `x64` machine, you can install the `x64` artifact. Click on the artifact to download it.

Once the artifact is downloaded, extract the contents of the zip file.

![Install](Assets/install.png)

Right click on the `Add-AppDevPackage.ps1` file and click `Run with PowerShell`. Grant the necessary permissions and the app will be installed on your local machine.

![Run](Assets/run.png)

> [!NOTE]
> This process may require you to enable `Developer Mode` on your machine. You can enable `Developer Mode` by going to `Settings` > `Update & Security` > `For developers` and then selecting `Developer mode`.

## Environment Setup

You need a working oracle database to run the application. Replace `ORACLE_CONNECTION_STRING` in `DatabaseService.cs` with your connection string. Replace `SUPABASE_KEY` and `SUPAEBASE_URL` in `StorageService.cs` with your supabase key and url respectively.
