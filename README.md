# NetworkImage

Supported image formats: PNG, JPEG, GIF, BMP, WEBP, SVG, TIFF, ICO and animated GIFs.

# Usage
1. Clone the repo
2. Open the project in VS Code or Visual Studio
3. Build and run the project

# Testing
1. Most image would load
2. For image resource provide arcgis image service url along with token in the code

# API
1. `NetworkImage` - The main class for loading images from a URL.

```
<local:NetworkImage
    HeightRequest="185"
    Url="C:\SomeFolder\NetworkImage\NetworkImage\Resources\Images\dotnet_bot.png"
    LoadThumbnail="True"
    Aspect="AspectFit"/>
```

```
<local:NetworkImage
    WidthRequest="100"
    HeightRequest="100"
    Token="3NKHt6i2urmWtqOuugvr9YL7jsJyBpbxO7NQ5als7dZBGdk_NfWg_nbT_FHPScHKptAA-Nc_yJ1MkDwLzdFz-pqDR-S0M4Q5e4Xtcs70XyKojH9lbofBUtqqSTM-Wj27urHHmXu_O2RCGAe6K8QJH5PZ813u0Voqgqh2Xafio60zDmGFCrZPKRSMNeKYug4pxyDaiaRozNvEvsgAV9VyqZg82Lf8QL53MoJc9B2lNQI"
    Url="https://www.arcgis.com/sharing/rest/content/items/17535a3ac5194de8bcbc0a6dfaef2288/resources/56k2Zlxu8vhp7gh1JAjzBT.png"
    RequestWidth="1000"
    PlaceholderImageUrl="esri_logo.png"
    LoadThumbnail="True"
    Aspect="AspectFit"/>
```
