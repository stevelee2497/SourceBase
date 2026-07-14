using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using SourceBase.Web.Auth;

namespace SourceBase.Web.Services;

public class ApiHttpClient(HttpClient http, BlazorAuthStateProvider auth, ToastService toast)
{
    private static readonly ErrorResponse UnknownError = new("UNKNOWN", "An unexpected error occurred.", string.Empty);

    private HttpRequestMessage Request(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (body is not null)
            req.Content = JsonContent.Create(body);
        return req;
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, object? body = null)
    {
        var req = Request(method, url, body);
        if (!string.IsNullOrWhiteSpace(auth.AccessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return req;
    }

    private async Task<(T? data, ErrorResponse? error)> ExecuteAsync<T>(Func<HttpRequestMessage> factory, bool retry = true, bool silent = false)
    {
        try
        {
            var response = await http.SendAsync(factory());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && retry)
            {
                if (!await TryRefreshTokensAsync())
                {
                    var err = await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
                    return (default, err);
                }
                response = await http.SendAsync(factory());
            }
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<T>(), null);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
            if (!silent) toast.ShowError(error);
            return (default, error);
        }
        catch
        {
            if (!silent) toast.ShowError(UnknownError);
            return (default, UnknownError);
        }
    }

    private async Task<ErrorResponse?> ExecuteAsync(Func<HttpRequestMessage> factory, bool retry = true, bool silent = false)
    {
        try
        {
            var response = await http.SendAsync(factory());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && retry)
            {
                if (!await TryRefreshTokensAsync())
                {
                    var err = await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
                    return err;
                }
                response = await http.SendAsync(factory());
            }
            if (response.IsSuccessStatusCode)
                return null;
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
            if (!silent) toast.ShowError(error);
            return error;
        }
        catch
        {
            if (!silent) toast.ShowError(UnknownError);
            return UnknownError;
        }
    }

    private async Task<bool> TryRefreshTokensAsync()
    {
        if (string.IsNullOrWhiteSpace(auth.RefreshToken))
        {
            await auth.SignOutAsync();
            return false;
        }
        var (tokens, _) = await ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/refresh", new { token = auth.RefreshToken }), retry: false, silent: true);
        if (tokens is null)
        {
            await auth.SignOutAsync();
            return false;
        }
        await auth.SetTokensAsync(tokens);
        return true;
    }

    // ── Auth (anonymous) ────────────────────────────────────────────────────

    public Task<(LoginResponse? data, ErrorResponse? error)> LoginAsync(string email, string password) =>
        ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/login", new { email, password }));

    public Task<ErrorResponse?> RegisterAsync(string userName, string email, string password, string? firstName = null, string? lastName = null, string? phoneNumber = null) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/register", new { userName, email, password, firstName, lastName, phoneNumber }));

    public Task<ErrorResponse?> ForgotPasswordAsync(string email) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/forgotPassword", new { email }));

    public Task<ErrorResponse?> ResetPasswordAsync(string email, string code, string newPassword) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/resetPassword", new { email, code, newPassword }));

    public Task<ErrorResponse?> ConfirmEmailAsync(string email, string code) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/confirmEmail", new { email, code }));

    // ── Auth (authenticated) ─────────────────────────────────────────────────

    public Task<(LoginResponse? data, ErrorResponse? error)> RefreshTokenAsync(string token) =>
        ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/refresh", new { token }), retry: false);

    public Task<(UserInfoResponse? data, ErrorResponse? error)> GetUserInfoAsync() =>
        ExecuteAsync<UserInfoResponse>(() => AuthorizedRequest(HttpMethod.Get, "/api/auth/info"));

    public Task<ErrorResponse?> LogoutAsync() =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/auth/logout"));

    public Task<(AvatarUploadUrlResponse? data, ErrorResponse? error)> GetAvatarUploadUrlAsync(string fileName) =>
        ExecuteAsync<AvatarUploadUrlResponse>(() => AuthorizedRequest(HttpMethod.Post, "/api/files/avatar/upload-url", new { fileName }));

    public async Task<string?> PerformAvatarUploadAsync(IBrowserFile file, AvatarUploadUrlResponse uploadInfo)
    {
        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue(uploadInfo.ContentType);
            content.Headers.ContentLength = file.Size;
            var putResponse = await http.PutAsync(uploadInfo.UploadUrl, content);
            if (!putResponse.IsSuccessStatusCode)
            {
                return "Failed to upload avatar. Please try again.";
            }
            return null;
        }
        catch
        {
            return "Failed to upload avatar. Please try again.";
        }
    }

    public Task<ErrorResponse?> UpdateUserInfoAsync(object fields) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, "/api/auth/info", fields));

    // ── Roles ────────────────────────────────────────────────────────────────

    public Task<(PagingResponse<RoleResponse>? data, ErrorResponse? error)> GetRolesAsync(int page, int limit) =>
        ExecuteAsync<PagingResponse<RoleResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/roles?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateRoleAsync(string name, string? description) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/roles", new { name, description }));

    public Task<ErrorResponse?> UpdateRoleAsync(Guid id, string? name = null, string? description = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/roles/{id}", new { name, description }));

    public Task<ErrorResponse?> DeleteRoleAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/roles/{id}"));

    // ── Users ────────────────────────────────────────────────────────────────

    public Task<(PagingResponse<UserResponse>? data, ErrorResponse? error)> GetUsersAsync(int page, int limit) =>
        ExecuteAsync<PagingResponse<UserResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/users?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateUserAsync(string userName, string email, string password, string? firstName, string? lastName, string? phoneNumber, string[] roles) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/users", new { userName, email, password, firstName, lastName, phoneNumber, roles }));

    public Task<ErrorResponse?> UpdateUserAsync(Guid id, string email, string? firstName, string? lastName, string? phoneNumber, string? avatarUrl, string[] roles) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, $"/api/users/{id}", new { email, firstName, lastName, phoneNumber, avatarUrl, roles }));

    public Task<ErrorResponse?> DeleteUserAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/users/{id}"));

    // ── Todos ────────────────────────────────────────────────────────────────

    public Task<(PagingResponse<TodoItemResponse>? data, ErrorResponse? error)> GetTodosAsync(int page, int limit, Guid? todoListId = null, string? orderBy = null, string? order = null)
    {
        var url = $"/api/todos?page={page}&limit={limit}";
        if (todoListId.HasValue)
            url += $"&todoListId={todoListId}";
        if (!string.IsNullOrWhiteSpace(orderBy))
            url += $"&orderBy={Uri.EscapeDataString(orderBy)}";
        if (!string.IsNullOrWhiteSpace(order))
            url += $"&order={Uri.EscapeDataString(order)}";
        return ExecuteAsync<PagingResponse<TodoItemResponse>>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    public Task<ErrorResponse?> CreateTodoAsync(string title, string date, string status, Guid? todoListId = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/todos", new { title, date, status, todoListId }));

    public Task<ErrorResponse?> UpdateTodoAsync(Guid id, string? title = null, string? date = null, string? status = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/todos/{id}", new { title, date, status }));

    public Task<ErrorResponse?> DeleteTodoAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/todos/{id}"));

    // ── TodoLists ────────────────────────────────────────────────────────────

    public Task<(PagingResponse<TodoListResponse>? data, ErrorResponse? error)> GetTodoListsAsync(int page, int limit) =>
        ExecuteAsync<PagingResponse<TodoListResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/todo-lists?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateTodoListAsync(string name) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/todo-lists", new { name }));

    public Task<ErrorResponse?> UpdateTodoListAsync(Guid id, string? name = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/todo-lists/{id}", new { name }));

    public Task<ErrorResponse?> DeleteTodoListAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/todo-lists/{id}"));

    // ── Stats ─────────────────────────────────────────────────────────────────

    public Task<(StatsResponse? data, ErrorResponse? error)> GetStatsAsync() =>
        ExecuteAsync<StatsResponse>(() => AuthorizedRequest(HttpMethod.Get, "/api/data/stats"));

    public Task<(RedisStatusResponse? data, ErrorResponse? error)> GetRedisStatusAsync() =>
        ExecuteAsync<RedisStatusResponse>(() => AuthorizedRequest(HttpMethod.Get, "/api/data/redis-status"));

    // ── User Admin Actions ───────────────────────────────────────────────────

    public Task<ErrorResponse?> ResetUserPasswordAsync(Guid id, string newPassword) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, $"/api/users/{id}/reset-password", new { newPassword }));

    public Task<ErrorResponse?> ConfirmUserEmailAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, $"/api/users/{id}/confirm-email"));

    // ── Wallets ──────────────────────────────────────────────────────────────

    public Task<(GetWalletsResponse? data, ErrorResponse? error)> GetWalletsAsync() =>
        ExecuteAsync<GetWalletsResponse>(() => AuthorizedRequest(HttpMethod.Get, "/api/wallets"));

    public Task<(WalletResponse? data, ErrorResponse? error)> GetWalletAsync(Guid id) =>
        ExecuteAsync<WalletResponse>(() => AuthorizedRequest(HttpMethod.Get, $"/api/wallets/{id}"));

    public Task<ErrorResponse?> CreateWalletAsync(string name, decimal initialBalance, string currency, string? icon) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/wallets", new { name, initialBalance, currency, icon }));

    public Task<ErrorResponse?> UpdateWalletAsync(Guid id, string? name = null, string? icon = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/wallets/{id}", new { name, icon }));

    public Task<ErrorResponse?> DeleteWalletAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/wallets/{id}"));

    public Task<ErrorResponse?> ConfigureWalletAsync(Guid id, string currency) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, $"/api/wallets/{id}/config", new { currency }));

    // ── Icons ────────────────────────────────────────────────────────────────

    public Task<(List<IconResponse>? data, ErrorResponse? error)> GetIconsAsync(string? group = null) =>
        ExecuteAsync<List<IconResponse>>(() => AuthorizedRequest(HttpMethod.Get,
            $"/api/icons{(string.IsNullOrWhiteSpace(group) ? string.Empty : $"?group={Uri.EscapeDataString(group)}")}"));

    public Task<ErrorResponse?> CreateIconAsync(string value, string name, string group, int sortOrder) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/icons", new { value, name, group, sortOrder }));

    public Task<ErrorResponse?> UpdateIconAsync(Guid id, string? value = null, string? name = null, string? group = null, int? sortOrder = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/icons/{id}", new { value, name, group, sortOrder }));

    public Task<ErrorResponse?> DeleteIconAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/icons/{id}"));

    public Task<(IconUploadUrlResponse? data, ErrorResponse? error)> GetIconUploadUrlAsync(string fileName) =>
        ExecuteAsync<IconUploadUrlResponse>(() => AuthorizedRequest(HttpMethod.Post, "/api/icons/upload-image", new { fileName }));

    public async Task<string?> PerformIconUploadAsync(IBrowserFile file, IconUploadUrlResponse uploadInfo)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(uploadInfo.ContentType);
        content.Headers.ContentLength = file.Size;
        var response = await http.PutAsync(uploadInfo.UploadUrl, content);
        return response.IsSuccessStatusCode ? uploadInfo.IconUrl : null;
    }

    // ── Categories ───────────────────────────────────────────────────────────

    public Task<(List<CategoryResponse>? data, ErrorResponse? error)> GetCategoriesAsync(string? type = null) =>
        ExecuteAsync<List<CategoryResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/categories{(string.IsNullOrWhiteSpace(type) ? string.Empty : $"?type={Uri.EscapeDataString(type)}")}"));

    public Task<ErrorResponse?> CreateCategoryAsync(string name, string type, string? icon) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/categories", new { name, type, icon }));

    public Task<ErrorResponse?> UpdateCategoryAsync(Guid id, string? name = null, string? icon = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/categories/{id}", new { name, icon }));

    public Task<ErrorResponse?> DeleteCategoryAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/categories/{id}"));

    // ── Transactions ─────────────────────────────────────────────────────────

    public Task<(PagingResponse<TransactionResponse>? data, ErrorResponse? error)> GetTransactionsAsync(int page, int limit, IEnumerable<Guid>? walletIds = null, string? type = null, string? dateFrom = null, string? dateTo = null, Guid? categoryId = null, bool excludeTransfers = false)
    {
        var url = $"/api/transactions?page={page}&limit={limit}";
        if (walletIds != null)
            url += string.Join(string.Empty, walletIds.Select(id => $"&walletIds={id}"));
        if (!string.IsNullOrWhiteSpace(type))
            url += $"&type={Uri.EscapeDataString(type)}";
        if (!string.IsNullOrWhiteSpace(dateFrom))
            url += $"&dateFrom={Uri.EscapeDataString(dateFrom)}";
        if (!string.IsNullOrWhiteSpace(dateTo))
            url += $"&dateTo={Uri.EscapeDataString(dateTo)}";
        if (categoryId.HasValue)
            url += $"&categoryId={categoryId}";
        if (excludeTransfers)
            url += $"&exclude=Transfer";
        return ExecuteAsync<PagingResponse<TransactionResponse>>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    public Task<ErrorResponse?> CreateTransactionAsync(Guid walletId, decimal amount, string type, string date, string? note, Guid categoryId) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/transactions", new { walletId, amount, type, date, note, categoryId }));

    public Task<ErrorResponse?> UpdateTransactionAsync(Guid id, decimal? amount = null, string? type = null, string? date = null, string? note = null, Guid? categoryId = null, Guid? walletId = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/transactions/{id}", new { amount, type, date, note, categoryId, walletId }));

    public Task<ErrorResponse?> DeleteTransactionAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/transactions/{id}"));

    public Task<(GetTransactionSummaryResponse? data, ErrorResponse? error)> GetTransactionSummaryAsync(Guid? walletId = null, string? dateFrom = null, string? dateTo = null)
    {
        var url = "/api/transactions/summary";
        var hasQuery = false;

        void Append(string key, string value)
        {
            url += hasQuery ? "&" : "?";
            url += $"{key}={Uri.EscapeDataString(value)}";
            hasQuery = true;
        }

        if (walletId.HasValue)
            Append("walletId", walletId.Value.ToString());
        if (!string.IsNullOrWhiteSpace(dateFrom))
            Append("dateFrom", dateFrom);
        if (!string.IsNullOrWhiteSpace(dateTo))
            Append("dateTo", dateTo);

        return ExecuteAsync<GetTransactionSummaryResponse>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    // ── Transfers ────────────────────────────────────────────────────────────

    public Task<(PagingResponse<TransferResponse>? data, ErrorResponse? error)> GetTransfersAsync(int page, int limit, Guid? walletId = null)
    {
        var url = $"/api/transfers?page={page}&limit={limit}";
        if (walletId.HasValue)
            url += $"&walletId={walletId}";
        return ExecuteAsync<PagingResponse<TransferResponse>>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    public Task<ErrorResponse?> CreateTransferAsync(Guid fromWalletId, Guid toWalletId, decimal amount, string date, string? note) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/transfers", new { fromWalletId, toWalletId, amount, date, note }));

    public Task<ErrorResponse?> DeleteTransferAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/transfers/{id}"));

    // ── TimeSheets ────────────────────────────────────────────────────────────

    public Task<(PagingResponse<TimeSheetItemResponse>? data, ErrorResponse? error)> GetTimeSheetsAsync(int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return ExecuteAsync<PagingResponse<TimeSheetItemResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/time-sheets?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&limit=200"));
    }

    public Task<(PagingResponse<TimeSheetItemResponse>? data, ErrorResponse? error)> GetTimeSheetsAsync(DateOnly date) =>
        ExecuteAsync<PagingResponse<TimeSheetItemResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/time-sheets?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}&limit=100"));

    public Task<(TimeSheetBulkResponse? data, ErrorResponse? error)> UpsertTimeSheetsAsync(List<TimeSheetUpsertItem> items) =>
        ExecuteAsync<TimeSheetBulkResponse>(() => AuthorizedRequest(HttpMethod.Post, "/api/time-sheets", new { items }));

    public Task<ErrorResponse?> DeleteTimeSheetAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/time-sheets/{id}"));

    // ── Notifications ─────────────────────────────────────────────────────────

    public Task<(GetNotificationsResponse? data, ErrorResponse? error)> GetNotificationsAsync(int page = 1, int limit = 50, bool unreadOnly = false) =>
        ExecuteAsync<GetNotificationsResponse>(() => AuthorizedRequest(HttpMethod.Get, $"/api/notifications?page={page}&limit={limit}&unreadOnly={unreadOnly}"));

    public Task<ErrorResponse?> MarkNotificationAsReadAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, $"/api/notifications/{id}/read"));

    public Task<ErrorResponse?> MarkAllNotificationsAsReadAsync() =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, "/api/notifications/read-all"));

    public Task<ErrorResponse?> ClearAllNotificationsAsync() =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, "/api/notifications"));

    // ── Habits ────────────────────────────────────────────────────────────────

    public Task<(List<HabitItemResponse>? data, ErrorResponse? error)> GetHabitsAsync() =>
        ExecuteAsync<List<HabitItemResponse>>(() => AuthorizedRequest(HttpMethod.Get, "/api/habits"));

    public Task<ErrorResponse?> CreateHabitAsync(string name, string? icon) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/habits", new { name, icon }));

    public Task<ErrorResponse?> UpdateHabitAsync(Guid id, string? name = null, string? icon = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/habits/{id}", new { name, icon }));

    public Task<ErrorResponse?> DeleteHabitAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/habits/{id}"));

    public Task<ErrorResponse?> LogHabitAsync(Guid habitId, string habitName, string action) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/habit-logs", new { entries = new[] { new { habitId, habitName, action, occurredAt = DateTime.UtcNow } } }));

    // ── Habit Logs ────────────────────────────────────────────────────────────

    public Task<(PagingResponse<HabitLogResponse>? data, ErrorResponse? error)> GetHabitLogsAsync(DateTime from, DateTime to)
    {
        var url = $"/api/habit-logs?from={Uri.EscapeDataString(from.ToString("o"))}&to={Uri.EscapeDataString(to.ToString("o"))}&limit=500&orderBy=OccurredAt&order=Asc";
        return ExecuteAsync<PagingResponse<HabitLogResponse>>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    // ── Gold Prices ───────────────────────────────────────────────────────────

    public Task<(PagingResponse<GoldPriceResponse>? data, ErrorResponse? error)> GetGoldPricesAsync(int page = 1, int limit = 20, string? source = null, string? dateFrom = null, string? dateTo = null, bool? latest = null)
    {
        var url = $"/api/gold-prices?page={page}&limit={limit}&order=Desc&orderBy=RecordedAt";
        if (!string.IsNullOrWhiteSpace(source)) url += $"&source={Uri.EscapeDataString(source)}";
        if (!string.IsNullOrWhiteSpace(dateFrom)) url += $"&dateFrom={Uri.EscapeDataString(dateFrom)}";
        if (!string.IsNullOrWhiteSpace(dateTo)) url += $"&dateTo={Uri.EscapeDataString(dateTo)}";
        if (latest is not null) url += $"&latest={latest.Value.ToString().ToLowerInvariant()}";
        return ExecuteAsync<PagingResponse<GoldPriceResponse>>(() => AuthorizedRequest(HttpMethod.Get, url));
    }

    // ── Machines ──────────────────────────────────────────────────────────────

    public Task<(PagingResponse<MachineResponse>? data, ErrorResponse? error)> GetMachinesAsync(int page = 1, int limit = 50) =>
        ExecuteAsync<PagingResponse<MachineResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/machines?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateMachineAsync(string name, string? status = null) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/machines", new { name, status }));

    public Task<ErrorResponse?> UpdateMachineAsync(Guid id, string? alias, string? status) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Patch, $"/api/machines/{id}", new { alias, status }));

    public Task<ErrorResponse?> DeleteMachineAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/machines/{id}"));

    public Task<ErrorResponse?> ShutdownMachineAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, $"/api/machines/{id}/shutdown", new { }));

    public Task<ErrorResponse?> RestartMachineAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, $"/api/machines/{id}/restart", new { }));
}

public sealed record PagingResponse<T>(List<T> Items, int Page, int Limit, int Total);
public sealed record AvatarUploadUrlResponse(string UploadUrl, string AvatarUrl, string ContentType);
public sealed record UserInfoResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl, Guid? DefaultTodoListId, string[] Roles);
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType);
public sealed record RoleResponse(Guid Id, string Name, string? Description);
public sealed record UserResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, bool EmailConfirmed, string? AvatarUrl, IEnumerable<string> Roles);
public sealed record TodoItemResponse(Guid Id, string Title, string Date, string Status, Guid? TodoListId);
public sealed record TodoListResponse(Guid Id, string Name, int ItemCount, DateTime? CreatedOn, string? CreatedBy, bool IsDefault);
public sealed record StatsResponse(int UserCount, int TotalTodoLists, int TotalTodoItems, int CompletedTodoItems, int TotalWallets, int TotalTransactions, decimal TotalBalance, decimal MonthlyIncome, decimal MonthlyExpense, bool AllLogged, string LogTimeDetail);
public sealed record RedisStatusResponse(bool IsOnline);
public sealed record ErrorResponse(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors = null);
public sealed record WalletResponse(Guid Id, string Name, decimal Balance, decimal InitialBalance, string Currency, string? Icon);
public sealed record GetWalletsResponse(List<WalletResponse> Wallets, decimal TotalBalance);
public sealed record GetWalletSummaryResponse(decimal TotalBalance, decimal MonthlyIncome, decimal MonthlyExpense, List<RecentTransactionResponse> RecentTransactions);
public sealed record RecentTransactionResponse(Guid Id, decimal Amount, string Type, string Date, string? Note, Guid WalletId, string WalletName, Guid? CategoryId, string? CategoryName);
public sealed record CategoryResponse(Guid Id, string Name, string Type, string? Icon, bool IsSystem);
public sealed record TransactionResponse(Guid Id, decimal Amount, string Type, string Date, string? Note, Guid WalletId, string WalletName, Guid? CategoryId, string? CategoryName, bool IsTransfer);
public sealed record GetTransactionSummaryResponse(decimal TotalIncome, decimal TotalExpense, decimal NetBalance, List<CategoryBreakdownResponse> ByCategory);
public sealed record CategoryBreakdownResponse(Guid? CategoryId, string? CategoryName, string Type, decimal Total);
public sealed record TransferResponse(Guid Id, Guid FromWalletId, string FromWalletName, Guid ToWalletId, string ToWalletName, decimal Amount, string Date, string? Note);
public sealed record TimeSheetItemResponse(Guid Id, string Date, string Project, decimal Hours);
public sealed record TimeSheetSummaryDayResponse(DateOnly Date, decimal TotalHours, List<string> Projects);
public sealed record TimeSheetUpsertItem(string Date, string Project, decimal Hours);
public sealed record TimeSheetBulkResponse(List<Guid> Ids);
public sealed record NotificationResponse(Guid Id, string Title, string Message, bool IsRead, DateTime? CreatedOn);
public sealed record GetNotificationsResponse(List<NotificationResponse> Items, int Page, int Limit, int Total);
public sealed record IconResponse(Guid Id, string Value, string Name, string Group, int SortOrder, bool IsSystem);
public sealed record IconUploadUrlResponse(string UploadUrl, string IconUrl, string ContentType);
public sealed record GoldPriceResponse(Guid Id, string Source, decimal BuyPrice, decimal SellPrice, DateTime RecordedAt);
public sealed record HabitLogResponse(Guid Id, string? HabitId, string? HabitName, string Action, DateTime OccurredAt, DateTime? CreatedOn);
public sealed record HabitItemResponse(Guid Id, string Name, string? Icon, bool IsSystem, int LogCount);
public sealed record MachineResponse(Guid Id, string Name, string? Alias, string Status, DateTime? LastReportedOn);
