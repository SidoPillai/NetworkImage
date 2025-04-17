using Microsoft.Maui.Controls;

namespace NetworkImageLibrary;

public partial class ImageLoaderPage : ContentPage
{
    // Ensure these controls are defined in your XAML file and linked to the code-behind
    private Entry _urlEntry;
    private CheckBox _loadThumbnailCheckBox;
    private Picker _aspectPicker;
    private Picker _cacheStrategyPicker;
    private Entry _requestWidthEntry;
    private Entry _tokenEntry;
    private NetworkImage _dynamicNetworkImage;

    public ImageLoaderPage()
    {
        InitializeComponent();

        // Initialize the controls if they are not automatically linked
        _urlEntry = this.FindByName<Entry>("UrlEntry");
        _loadThumbnailCheckBox = this.FindByName<CheckBox>("LoadThumbnailCheckBox");
        _aspectPicker = this.FindByName<Picker>("AspectPicker");
        _cacheStrategyPicker = this.FindByName<Picker>("CacheStrategyPicker");
        _requestWidthEntry = this.FindByName<Entry>("RequestWidthEntry");
        _tokenEntry = this.FindByName<Entry>("TokenEntry");
        _dynamicNetworkImage = this.FindByName<NetworkImage>("DynamicNetworkImage");
    }

    private void OnLoadImageClicked(object sender, EventArgs e)
    {
        // Existing code remains unchanged
        string url = _urlEntry.Text;
        bool loadThumbnail = _loadThumbnailCheckBox.IsChecked;

        // CacheStrategy
        string selectedCacheStrategy = _cacheStrategyPicker.SelectedItem?.ToString() ?? "None";
        CacheStrategy cacheStrategy = CacheStrategy.None;
        if (Enum.TryParse(selectedCacheStrategy, out CacheStrategy parsedCacheStrategy))
        {
            cacheStrategy = parsedCacheStrategy;
        }

        string selectedAspect = _aspectPicker.SelectedItem?.ToString();
        Aspect aspect = Aspect.AspectFit;
        if (!string.IsNullOrEmpty(selectedAspect))
        {
            aspect = Enum.Parse<Aspect>(selectedAspect);
        }
        int requestWidth = 0;
        if (int.TryParse(_requestWidthEntry.Text, out int parsedWidth))
        {
            requestWidth = parsedWidth;
        }
        string token = _tokenEntry.Text;

        _dynamicNetworkImage.Url = url;
        _dynamicNetworkImage.CacheStrategy = cacheStrategy;
        _dynamicNetworkImage.LoadThumbnail = loadThumbnail;
        _dynamicNetworkImage.Aspect = aspect;

        if (requestWidth > 0)
        {
            _dynamicNetworkImage.WidthRequest = requestWidth;
        }

        if (!string.IsNullOrEmpty(token))
        {
            _dynamicNetworkImage.Token = token;
        }
    }
}