using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KidsParadiseByShoptick.AdminApp.Helpers;
using KidsParadiseByShoptick.AdminApp.Models;
using KidsParadiseByShoptick.AdminApp.Services;

namespace KidsParadiseByShoptick.AdminApp.ViewModels;

public partial class AffiliatesViewModel : ObservableObject
{
    private const int PageSize = 30;

    private readonly AdminApiService _api;
    private readonly AuthSession _session;
    private readonly PagedListLoadCoordinator _load = new();
    private CancellationTokenSource? _searchDebounce;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool isLoadingMore;

    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    private bool hasMoreItems;

    public ObservableCollection<AffiliatePartnerModel> Items { get; } = [];

    public AffiliatesViewModel(AdminApiService api, AuthSession session)
    {
        _api = api;
        _session = session;
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (!_session.IsLoggedIn || Items.Count > 0 || IsBusy || IsLoadingMore)
            return;
        await ReloadAsync();
    }

    bool CanLoad() => PagedListLoadCoordinator.CanReload(IsBusy, IsLoadingMore);

    [RelayCommand(CanExecute = nameof(CanLoad))]
    async Task LoadAsync()
    {
        IsRefreshing = true;
        try { await ReloadAsync(); }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    async Task ReloadAsync()
    {
        await _load.RunExclusiveAsync(async () =>
        {
            _load.BeginReload();
            HasMoreItems = false;
            ErrorMessage = null;
            StatusText = string.Empty;
            Items.Clear();
            LoadMoreCommand.NotifyCanExecuteChanged();
            await LoadNextPageCoreAsync();
        });
    }

    bool CanLoadMore() => PagedListLoadCoordinator.CanLoadMore(HasMoreItems, IsBusy, IsLoadingMore, Items.Count);

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    async Task LoadMoreAsync()
    {
        await _load.RunExclusiveAsync(async () =>
        {
            if (!PagedListLoadCoordinator.CanLoadMore(HasMoreItems, IsBusy, IsLoadingMore, Items.Count))
                return;
            await LoadNextPageCoreAsync();
        });
    }

    async Task LoadNextPageCoreAsync()
    {
        try
        {
            var isFirst = Items.Count == 0;
            if (isFirst) IsBusy = true;
            else IsLoadingMore = true;

            var page = (Items.Count / PageSize) + 1;
            var result = await _api.GetAffiliatesPagedAsync(
                page, PageSize, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim());

            foreach (var item in result.Items)
                Items.Add(item);

            HasMoreItems = Items.Count < result.TotalCount;
            StatusText = result.TotalCount == 0
                ? "No affiliate partners"
                : $"Showing {Items.Count} of {result.TotalCount}";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
            LoadMoreCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        var token = _searchDebounce.Token;
        _ = DebounceReloadAsync(token);
    }

    async Task DebounceReloadAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(350, token);
            if (!token.IsCancellationRequested)
                await ReloadAsync();
        }
        catch (TaskCanceledException) { }
    }

    [RelayCommand]
    async Task AddAsync() => await Shell.Current.GoToAsync("affiliate-edit");

    [RelayCommand]
    async Task EditAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"affiliate-edit?id={item.Id}");
    }

    [RelayCommand]
    async Task LedgerAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        await Shell.Current.GoToAsync($"affiliate-ledger?id={item.Id}");
    }

    [RelayCommand]
    async Task CopyLinkAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        await Clipboard.Default.SetTextAsync(item.ReferralLink);
        await Shell.Current.DisplayAlert("Copied", "Affiliate link copied to clipboard.", "OK");
    }

    [RelayCommand]
    async Task OpenWhatsAppAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        if (string.IsNullOrWhiteSpace(item.Whatsapp))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "Partner WhatsApp number is missing.", "OK");
            return;
        }

        try
        {
            await AffiliateWhatsAppHelper.OpenPartnerChatAsync(item);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task CopyMessageAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        if (string.IsNullOrWhiteSpace(item.Whatsapp))
        {
            await Clipboard.Default.SetTextAsync(item.PartnerWelcomeMessage);
            await Shell.Current.DisplayAlert("Copied", "Partner welcome message copied to clipboard.", "OK");
            return;
        }

        try
        {
            await AffiliateWhatsAppHelper.OpenWelcomeChatAsync(item);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task DeleteAsync(AffiliatePartnerModel? item)
    {
        if (item is null) return;
        var ok = await Shell.Current.DisplayAlert(
            "Delete partner?",
            $"Delete {item.Name}? Partners with orders cannot be deleted.",
            "Delete", "Cancel");
        if (!ok) return;
        try
        {
            await _api.DeleteAffiliateAsync(item.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

public partial class AffiliateEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int? _id;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenWhatsApp))]
    [NotifyCanExecuteChangedFor(nameof(OpenWhatsAppCommand))]
    private string whatsapp = string.Empty;
    [ObservableProperty] private string accountNumber = string.Empty;
    [ObservableProperty] private string walletBankName = string.Empty;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string title = "New Affiliate";
    [ObservableProperty] private string? referralLink;
    [ObservableProperty] private AffiliatePartnerModel? partner;

    public bool CanOpenWhatsApp => !string.IsNullOrWhiteSpace(Whatsapp);

    public AffiliateEditViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
            _id = id;
        Title = _id.HasValue ? "Edit Affiliate" : "New Affiliate";
    }

    [RelayCommand]
    async Task AppearingAsync()
    {
        if (!_id.HasValue) return;
        try
        {
            IsBusy = true;
            var loaded = await _api.GetAffiliateAsync(_id.Value);
            Partner = loaded;
            Name = loaded.Name;
            Whatsapp = loaded.Whatsapp ?? string.Empty;
            AccountNumber = loaded.AccountNumber ?? string.Empty;
            WalletBankName = loaded.WalletBankName ?? string.Empty;
            Code = loaded.Code;
            IsActive = loaded.IsActive;
            ReferralLink = loaded.ReferralLink;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task CopyLinkAsync()
    {
        if (string.IsNullOrWhiteSpace(ReferralLink)) return;
        await Clipboard.Default.SetTextAsync(ReferralLink);
        await Shell.Current.DisplayAlert("Copied", "Affiliate link copied.", "OK");
    }

    [RelayCommand(CanExecute = nameof(CanOpenWhatsApp))]
    async Task OpenWhatsAppAsync()
    {
        if (string.IsNullOrWhiteSpace(Whatsapp))
        {
            await Shell.Current.DisplayAlert("WhatsApp", "Partner WhatsApp number is missing.", "OK");
            return;
        }

        try
        {
            var model = Partner ?? new AffiliatePartnerModel();
            model.Name = string.IsNullOrWhiteSpace(Name) ? model.Name : Name.Trim();
            model.Whatsapp = Whatsapp.Trim();
            model.AccountNumber = string.IsNullOrWhiteSpace(AccountNumber) ? model.AccountNumber : AccountNumber.Trim();
            model.WalletBankName = string.IsNullOrWhiteSpace(WalletBankName) ? model.WalletBankName : WalletBankName.Trim();
            model.Code = string.IsNullOrWhiteSpace(Code) ? model.Code : Code;
            model.IsActive = IsActive;
            await AffiliateWhatsAppHelper.OpenPartnerChatAsync(model);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand]
    async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Validation", "Name is required.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(Whatsapp))
        {
            await Shell.Current.DisplayAlert("Validation", "WhatsApp is required.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(AccountNumber))
        {
            await Shell.Current.DisplayAlert("Validation", "Account number is required.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(WalletBankName))
        {
            await Shell.Current.DisplayAlert("Validation", "Wallet / Bank name is required.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var payload = new
            {
                name = Name.Trim(),
                whatsapp = Whatsapp.Trim(),
                accountNumber = AccountNumber.Trim(),
                walletBankName = WalletBankName.Trim(),
                isActive = IsActive,
            };

            AffiliatePartnerModel saved;
            if (_id.HasValue)
                saved = await _api.UpdateAffiliateAsync(_id.Value, payload);
            else
                saved = await _api.CreateAffiliateAsync(payload);

            Code = saved.Code;
            ReferralLink = saved.ReferralLink;
            await Shell.Current.DisplayAlert(
                "Saved",
                $"Partner saved.\nOpaque link:\n{saved.ReferralLink}",
                "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
public partial class AffiliateLedgerViewModel : ObservableObject, IQueryAttributable
{
    private readonly AdminApiService _api;
    private int _id;

    [ObservableProperty] private string title = "Ledger";
    [ObservableProperty] private string partnerName = string.Empty;
    [ObservableProperty] private string summaryText = string.Empty;
    [ObservableProperty] private string balanceText = string.Empty;
    [ObservableProperty] private string balanceColor = "#0f766e";
    [ObservableProperty] private string paymentAmount = string.Empty;
    [ObservableProperty] private string paymentNotes = string.Empty;
    [ObservableProperty] private string paymentHint = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordPaymentCommand))]
    private bool canRecordPayment;
    [ObservableProperty] private decimal outstandingBalance;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenWhatsApp))]
    [NotifyCanExecuteChangedFor(nameof(OpenWhatsAppCommand))]
    private AffiliateLedgerModel? ledger;

    public bool CanOpenWhatsApp => !string.IsNullOrWhiteSpace(Ledger?.Partner.Whatsapp);

    public ObservableCollection<AffiliateLedgerEntryModel> Entries { get; } = [];

    public AffiliateLedgerViewModel(AdminApiService api) => _api = api;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var idObj) && int.TryParse(idObj?.ToString(), out var id))
            _id = id;
    }

    [RelayCommand]
    async Task AppearingAsync() => await ReloadAsync();

    [RelayCommand]
    async Task ReloadAsync()
    {
        if (_id <= 0) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var loaded = await _api.GetAffiliateLedgerAsync(_id);
            Ledger = loaded;
            PartnerName = loaded.Partner.Name;
            Title = $"{loaded.Partner.Name} Ledger";
            SummaryText =
                $"Net commission: Rs. {loaded.TotalCommission:N2} · Paid: Rs. {loaded.TotalPaid:N2}";
            OutstandingBalance = loaded.Balance;
            CanRecordPayment = loaded.Balance > 0;
            PaymentHint = CanRecordPayment
                ? $"Max payable now: Rs. {loaded.Balance:N2}"
                : string.Empty;
            if (loaded.Balance < 0)
            {
                BalanceText = $"Overpaid: Rs. {Math.Abs(loaded.Balance):N2}";
                BalanceColor = "#b91c1c";
            }
            else if (loaded.Balance == 0)
            {
                BalanceText = "Outstanding: Rs. 0.00 (settled)";
                BalanceColor = "#64748b";
            }
            else
            {
                BalanceText = $"Outstanding: Rs. {loaded.Balance:N2}";
                BalanceColor = "#0f766e";
            }

            Entries.Clear();
            foreach (var entry in loaded.Entries)
                Entries.Add(entry);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenWhatsApp))]
    async Task OpenWhatsAppAsync()
    {
        if (Ledger is null) return;
        try
        {
            await AffiliateWhatsAppHelper.OpenLedgerChatAsync(Ledger);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("WhatsApp", ex.Message, "OK");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRecordPayment))]
    async Task RecordPaymentAsync()
    {
        if (!TryParseAmount(PaymentAmount, out var amount) || amount <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Enter a valid payment amount.", "OK");
            return;
        }

        amount = decimal.Round(amount, 2);
        if (amount > OutstandingBalance)
        {
            await Shell.Current.DisplayAlert(
                "Validation",
                $"Amount cannot exceed outstanding balance (Rs. {OutstandingBalance:N2}).",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            await _api.RecordAffiliatePaymentAsync(
                _id, amount, string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim());
            PaymentAmount = string.Empty;
            PaymentNotes = string.Empty;
            await ReloadAsync();
            await Shell.Current.DisplayAlert("Saved", "Payment recorded in ledger.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var raw = text.Trim().Replace(",", "");
        return decimal.TryParse(
            raw,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out amount);
    }
}
