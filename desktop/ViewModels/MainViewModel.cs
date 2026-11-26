using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CompanyBrain.Client.Api;

namespace CompanyBrain.Client.ViewModels;

public sealed record DevicePreview(
    string Id, string Model, string Status, string SerialNumber,
    string OperatingSystem, string Site, string? TicketId, string TicketSummary)
{
    public bool HasTicket => TicketId is not null;
}

public partial class MainViewModel : ViewModelBase
{
    public ApiClient? Api { get; }

    public IReadOnlyList<DevicePreview> Devices { get; } =
    [
        new("LT-1042", "Dell Latitude 5450", "In use", "DL5450-8821",
            "Windows 11 Pro", "Central Office", "TCK-2041", "Won’t start (Open)"),
        new("LT-1028", "Lenovo ThinkPad T14", "Spare", "LNT14-6314",
            "Windows 11 Pro", "Central Office", null, "No open tickets")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDevices), nameof(IsCustomers), nameof(IsGuides), nameof(IsAsk))]
    public partial string Section { get; set; } = "Devices";

    [ObservableProperty]
    public partial DevicePreview SelectedDevice { get; set; }

    [ObservableProperty]
    public partial string DeviceQuery { get; set; } = "LT-1042";

    [ObservableProperty]
    public partial string GuideQuery { get; set; } = "loan";

    [ObservableProperty]
    public partial string Question { get; set; } = "Does LT-1042 qualify for a loan laptop on Friday evening?";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotice))]
    public partial string? Notice { get; set; }

    public bool IsDevices => Section == "Devices";
    public bool IsCustomers => Section == "Customers";
    public bool IsGuides => Section == "Guides";
    public bool IsAsk => Section == "Ask";
    public bool HasNotice => !string.IsNullOrEmpty(Notice);

    public MainViewModel() => SelectedDevice = Devices[0];

    public MainViewModel(ApiClient api) : this() => Api = api;

    [RelayCommand]
    private void Navigate(string? section)
    {
        if (section is not ("Devices" or "Customers" or "Guides" or "Ask")) return;
        Section = section;
        Notice = null;
    }

    [RelayCommand]
    private void Placeholder(string? action) => Notice = action switch
    {
        "PDF" => "Agreement PDF · document preview placeholder.",
        "Tickets" => "TCK-2041 · Won’t start · Open. Ticket details are a placeholder.",
        "Ask" => "This preview shows a sample answer and sample sources.",
        "AfterHours" => "After-hours support · guide preview placeholder.",
        _ => "This is a visual preview. Search is a placeholder."
    };

    [RelayCommand]
    private void DismissNotice() => Notice = null;
}
