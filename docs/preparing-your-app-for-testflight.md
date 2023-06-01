# Preparing your App for TestFlight

This guide shows the process of getting started with using Transporter and the App Connect site to upload your app to the App Store for internal testing via TestFlight. This guide will use the example of publishing a MacCatalyst app to the Mac App Store, but the process is similar for iOS and tvOS apps.

There is some preparation involved with preparing your Apple Developer account for publishing, such as creating an App Identifier and a Provisioning Profile. The MAUI documentation has [a great guide for that](https://aka.ms/maui-publish-app-store).

Also, your project will need to have certain configuration values set in the .csproj file, the Info.plist file, and the Entitlements.plist file.

## Preparing your Project File (.csproj) for App Store submission

Before you can submit your app to the App Store, you need to make sure that it is correctly configured. This includes:

- Setting the build to reference the correct `Entitlements.plist` in your project (For example, one for Debug and one for Release).
- Setting the correct `ApplicationId` in your project.
- Setting the correct `ApplicationVersion` in your project.
- Setting the correct `CodesignKey` in your project.
- Setting the correct `PackagingSigningKey` in your project.
- Setting the correct `CodesignEntitlements` in your project.
- Setting the correct `CodesignProvision` in your project.

Here's an example within a project file that is correctly configured for publishing a MacCatalyst app to the Mac App Store using NET 7.0:

```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net7.0-maccatalyst</TargetFramework>
    <RuntimeIdentifiers>maccatalyst-x64;maccatalyst-arm64</RuntimeIdentifiers>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>true</ImplicitUsings>
    <SupportedOSPlatformVersion>14.2</SupportedOSPlatformVersion>
    <ApplicationTitle>YourAppName</ApplicationTitle>
    <ApplicationId>com.yourcompany.yourappname</ApplicationId>
    <ApplicationVersion>0.1.0</ApplicationVersion>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <CreatePackage>false</CreatePackage>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
      <EnableCodeSigning>True</EnableCodeSigning>
      <CodesignEntitlements>Entitlements.plist</CodesignEntitlements>
      <EnablePackageSigning>true</EnablePackageSigning>
      <CreatePackage>true</CreatePackage>
      <CodesignKey>Apple Development: YOURNAME (*******)</CodesignKey>
      <CodesignProvision>YOUR PROFILE NAME</CodesignProvision>
      <PackageSigningKey>3rd Party Mac Developer Installer: YOURNAME (*******)</PackageSigningKey>
  </PropertyGroup>
</Project>
```

### Info.plist

The `Info.plist` file is used by the build system to configure your app and is already generated by your project. It contains info such as the Bundle Display Name and Bundle Identifier. (Please note that values such as the Bundle Identifier are set automatically with the values assigned above and do not have to be set manually. For more information about this, see [this guide](https://github.com/xamarin/xamarin-android/blob/main/Documentation/guides/OneDotNetSingleProject.md#ios-template))
### Entitlements.plist

The `Entitlements.plist` file is used by the build system to sign your app. Please see Apple's documentation on Entitlements [here](https://developer.apple.com/documentation/bundleresources/entitlements?language=objc).

Such common values include:

- com.apple.security.app-sandbox: Set this to true when working with MacOS or MacCatalyst apps.
- com.apple.security.network.client: Set this to let the forementioned sandboxed app connect to a server process running on the same/another machine (this is useful for web developer tools).

## Building your App for Publishing

Build your app for publishing. You can do this by using the dotnet build command. Here is an example for building a MacCatalyst app with .NET 7.0:

`dotnet build -f:net7.0-maccatalyst -c:Release`

## App Store Connect

App Store Connect is the App Store management site. You can use it to to upload your app to the App Store, and to manage your apps.

### Creating an App in App Store Connect

Before you can upload your app to the App Store, you need to create an app on App Store Connect. You can create an app on App Store Connect by following these steps:

1. Go to [https://appstoreconnect.apple.com](https://appstoreconnect.apple.com).
2. Click on "My Apps".
3. Click on the "+" button.
4. Enter the name of your app.
5. Select the platform for your app.
6. Select the primary language for your app.
7. Select the category for your app. Note: It is important that the primary category you select here will be the one 
8. Click on "Create".

### Uploading your App using Transporter

Once you have created an app on App Store Connect, you can upload your app to the App Store by following these steps:

1. Go to the Mac App Store and download the Transporter app.
2. Open the Transporter app.
3. Click on "Add".
4. Click on "Add file".
5. Select the `.ipa` or `.pkg` file that you want to upload.
6. Click on "Open".
7. Click on "Upload".

### Setting up an Internal Test Flight

Once you have uploaded your app to the App Store, you can set up an internal Test Flight. You can set up an internal Test Flight by following these steps:

1. Go to [https://appstoreconnect.apple.com](https://appstoreconnect.apple.com).
2. Click on "My Apps".
3. Click on the app that you want to set up an internal Test Flight for.
4. Click on "TestFlight".
5. Click on "Create Beta Group".
6. Enter the name of the internal Test Flight.
7. Click on "Create".

Note: Careful to add testers to the group directly, and not to an individual build. There seems to be an issue with Apple incorrectly assuming that the XCode version was a beta build and will refuse to add testers to the build.

### Adding Testers to an Internal Test Flight

Once you have set up an internal Test Flight, you can add testers to the internal Test Flight. You can add testers to the internal Test Flight by following these steps:

1. Go to [https://appstoreconnect.apple.com](https://appstoreconnect.apple.com).
2. Click on "My Apps".
3. Click on the app that you want to add testers to.
4. Click on "TestFlight".
5. Click on the internal Test Flight that you want to add testers to.
6. Click on "Add Testers".
7. Click on "Add by Email".
8. Enter the email address of the tester.
9. Click on "Add".

### Adding a Build to an Internal Test Flight

Once you have added testers to an internal Test Flight, you can add a build to the internal Test Flight. You can add a build to the internal Test Flight by following these steps:

1. Go to [https://appstoreconnect.apple.com](https://appstoreconnect.apple.com).
2. Click on "My Apps".
3. Click on the app that you want to add a build to.
4. Click on "TestFlight".
5. Click on the internal Test Flight that you want to add a build to.
6. Click on "Builds".
7. Click on "Add Build".
8. Select the build that you want to add.
9. Click on "Add".

### Distributing an Internal Test Flight

Once you have added a build to an internal Test Flight, you can distribute the internal Test Flight. You can distribute the internal Test Flight by following these steps:

1. Go to [https://appstoreconnect.apple.com](https://appstoreconnect.apple.com).
2. Click on "My Apps".
3. Click on the app that you want to distribute.
4. Click on "TestFlight".
5. Click on the internal Test Flight that you want to distribute.
6. Click on "Distribute External Testers".
7. Click on "Distribute".

### Downloading an Internal Test Flight

Once you have distributed an internal Test Flight, you can download the internal Test Flight. You can download the internal Test Flight by following these steps:

1. Download the "TestFlight" app from the Mac App Store.
2. Open the "TestFlight" app.
3. Click on "Sign In".
4. Enter your Apple ID.
5. Enter your password.
6. Click on "Sign In".
7. Click on the app that you want to download.
8. Click on "Download".

### Submitting new builds to Internal Test Flight

Fortunately, the process of submitting new builds to an internal Test Flight is very simple. You can submit new builds to an internal Test Flight by following these steps:

1. Increment the build number in your project by changing the Bundle Version in the Info.plist file.
2. Build your app.
3. Upload your app to the App Store via Transporter.
4. Open the "TestFlight" app. You should see an Update option for your app.