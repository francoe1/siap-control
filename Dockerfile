# Usa una imagen base con Windows y .NET Framework 4.8
FROM mcr.microsoft.com/windows/servercore:ltsc2019

# Instala .NET Framework 4.8
RUN powershell -Command `
    Start-Process -Wait -FilePath 'https://download.visualstudio.microsoft.com/download/pr/8284c48d-4a00-4a78-b5d4-2d54b0f21a43/6f07b8a62a8425d4e7b07160761e00e2/dotnet-framework-4.8-runtime.exe' -ArgumentList '/quiet /norestart'

# Instala NuGet
RUN powershell -Command `
    Invoke-WebRequest -Uri https://dist.nuget.org/win-x86/latest/nuget.exe -OutFile nuget.exe

# Copia los archivos del proyecto al contenedor
WORKDIR /app
COPY . .

# Instala MSBuild
RUN powershell -Command `
    Start-Process -Wait -FilePath 'https://download.visualstudio.microsoft.com/download/pr/fc5e7e87-40aa-464a-8780-104b7f00a6cb/3d37a682452a5e8c946d7338b2a0c0a2/vs_buildtools.exe' -ArgumentList '--quiet --norestart --add Microsoft.VisualStudio.Workload.MSBuildTools'

ENTRYPOINT ["powershell"]
