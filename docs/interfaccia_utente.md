# Progettazione Interfaccia Utente con MudBlazor

## Panoramica

Questo documento descrive la progettazione dell'interfaccia utente per il sistema WebUI multitenant di SciPhi AI R2R, implementata utilizzando Microsoft .NET 9.0 Blazor con componenti MudBlazor. L'interfaccia è progettata per essere intuitiva, reattiva e conforme alle best practice di UX enterprise, supportando i tre ruoli principali: Admin, Company e User.

## Principi di Design

La progettazione dell'interfaccia utente segue i seguenti principi:

1. **Coerenza**: Esperienza utente coerente in tutte le sezioni dell'applicazione
2. **Reattività**: Design responsive per desktop e dispositivi mobili
3. **Accessibilità**: Conformità agli standard WCAG 2.1 AA
4. **Efficienza**: Minimizzazione dei click per operazioni comuni
5. **Feedback**: Feedback chiaro per tutte le azioni utente
6. **Personalizzazione**: Supporto per temi e preferenze utente

## Struttura dell'Applicazione

L'applicazione è strutturata secondo un layout principale con navigazione laterale e aree di contenuto dinamiche.

### Layout Principale

```
┌─────────────────────────────────────────────────────────────────┐
│ Header (Logo, Titolo, Notifiche, Profilo)                       │
├────────────┬────────────────────────────────────────────────────┤
│            │                                                    │
│            │                                                    │
│            │                                                    │
│  Sidebar   │                 Content Area                       │
│  Navigation│                                                    │
│            │                                                    │
│            │                                                    │
│            │                                                    │
├────────────┴────────────────────────────────────────────────────┤
│ Footer (Copyright, Links, Version)                              │
└─────────────────────────────────────────────────────────────────┘
```

### Componenti Principali

1. **MainLayout.razor**: Layout principale dell'applicazione
2. **NavMenu.razor**: Menu di navigazione laterale dinamico basato sul ruolo
3. **AppBar.razor**: Barra superiore con logo, titolo e controlli utente
4. **LoginDisplay.razor**: Componente per visualizzare informazioni utente e logout
5. **ThemeProvider.razor**: Provider per gestione temi e personalizzazione

## Temi e Stili

L'applicazione supporta temi chiari e scuri, con personalizzazione dei colori primari e secondari.

### Configurazione MudBlazor

```csharp
// MudBlazorThemeProvider.razor
<MudThemeProvider Theme="@_currentTheme" />

@code {
    private MudTheme _currentTheme = new MudTheme
    {
        Palette = new Palette
        {
            Primary = "#1976D2",
            Secondary = "#388E3C",
            AppbarBackground = "#1976D2",
            Background = Colors.Grey.Lighten5,
            DrawerBackground = "#FFF",
            DrawerText = "rgba(0,0,0,0.7)",
            Success = "#4CAF50"
        },
        Typography = new Typography
        {
            Default = new Default
            {
                FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = 400,
                LineHeight = 1.43,
                LetterSpacing = ".01071em"
            },
            H1 = new H1
            {
                FontSize = "2rem",
                FontWeight = 500,
                LineHeight = 1.6
            }
            // Altri stili tipografici...
        }
    };
}
```

## Flussi di Navigazione

### Flusso di Login e Autenticazione

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│             │     │             │     │             │
│  Login Page │────►│ MFA (se     │────►│ Dashboard   │
│             │     │ abilitato)  │     │ (per ruolo) │
│             │     │             │     │             │
└─────────────┘     └─────────────┘     └─────────────┘
```

### Navigazione per Ruolo Admin

```
┌─────────────┐
│ Dashboard   │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│                                                     │
├─────────────┬─────────────┬─────────────┬──────────┤
│ Companies   │ Users       │ Settings    │ Logs     │
│ Management  │ Management  │ Management  │ & Audit  │
└─────────────┴─────────────┴─────────────┴──────────┘
```

### Navigazione per Ruolo Company

```
┌─────────────┐
│ Dashboard   │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│                                                     │
├─────────────┬─────────────┬─────────────┬──────────┤
│ Users       │ Collections │ Monitoring  │ Company  │
│ Management  │ Explorer    │ & Analytics │ Settings │
└─────────────┴─────────────┴─────────────┴──────────┘
```

### Navigazione per Ruolo User

```
┌─────────────┐
│ Dashboard   │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────────────────┐
│                                                     │
├─────────────┬─────────────┬─────────────┬──────────┤
│ Collections │ Documents   │ Chatbot     │ User     │
│ Management  │ Management  │ Interface   │ Settings │
└─────────────┴─────────────┴─────────────┴──────────┘
```

## Componenti UI per Ruolo

### Componenti Admin

1. **CompanyManagement.razor**: Gestione delle aziende/tenant
   - Creazione, modifica, disattivazione di Companies
   - Assegnazione di logo e configurazioni

2. **UserManagement.razor**: Gestione degli utenti di sistema
   - Creazione, modifica, disattivazione di utenti
   - Assegnazione di ruoli e permessi

3. **SystemSettings.razor**: Configurazione globale del sistema
   - Impostazioni generali
   - Configurazione R2R API
   - Limiti e quote globali

4. **SystemMonitoring.razor**: Monitoraggio del sistema
   - Dashboard con metriche di sistema
   - Visualizzazione log e audit trail
   - Stato delle code e dei servizi

### Componenti Company

1. **CompanyDashboard.razor**: Dashboard specifica per Company
   - Metriche di utilizzo
   - Attività recenti
   - Stato delle collezioni

2. **CompanyUserManagement.razor**: Gestione utenti della Company
   - Creazione, modifica, disattivazione di utenti
   - Assegnazione di ruoli interni

3. **CollectionExplorer.razor**: Esplorazione delle collezioni
   - Visualizzazione di tutte le collezioni della Company
   - Ricerca e filtro per utente, data, tipo

4. **CompanySettings.razor**: Impostazioni specifiche della Company
   - Configurazione LLM
   - Limiti e quote per utenti
   - Personalizzazione interfaccia

### Componenti User

1. **UserDashboard.razor**: Dashboard personale dell'utente
   - Collezioni recenti
   - Documenti recenti
   - Conversazioni recenti

2. **CollectionManagement.razor**: Gestione delle collezioni
   - Creazione, modifica, eliminazione di collezioni
   - Condivisione di collezioni

3. **DocumentManagement.razor**: Gestione dei documenti
   - Upload, visualizzazione, eliminazione di documenti
   - Monitoraggio stato elaborazione

4. **ChatInterface.razor**: Interfaccia chatbot
   - Selezione collezioni da interrogare
   - Interfaccia conversazionale
   - Visualizzazione risposte con citazioni

## Componenti Condivisi

### Dialoghi e Modali

1. **ConfirmDialog.razor**: Dialogo di conferma per azioni critiche
2. **UploadDialog.razor**: Dialogo per upload file con drag & drop
3. **ProgressDialog.razor**: Dialogo con indicatore di progresso
4. **NotificationPanel.razor**: Pannello per notifiche di sistema

### Componenti di Visualizzazione

1. **DataGrid.razor**: Griglia dati avanzata con ordinamento, filtro, paginazione
2. **DocumentViewer.razor**: Visualizzatore documenti con evidenziazione
3. **ChartComponent.razor**: Componente per visualizzazione grafici
4. **StatusBadge.razor**: Badge per visualizzare stati (success, warning, error)

## Implementazione delle Schermate Principali

### Login e Autenticazione

```razor
@page "/login"
@inject IAuthService AuthService
@inject NavigationManager NavigationManager

<MudContainer MaxWidth="MaxWidth.Small" Class="pa-4">
    <MudCard>
        <MudCardHeader>
            <MudText Typo="Typo.h5">Login</MudText>
        </MudCardHeader>
        <MudCardContent>
            <MudTextField @bind-Value="model.Username" Label="Username" Variant="Variant.Outlined" Class="mb-3" />
            <MudTextField @bind-Value="model.Password" Label="Password" Variant="Variant.Outlined" InputType="InputType.Password" />
            <MudCheckBox @bind-Checked="model.RememberMe" Label="Remember me" Color="Color.Primary" Class="mt-3" />
        </MudCardContent>
        <MudCardActions>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="ProcessLogin" FullWidth="true">Login</MudButton>
        </MudCardActions>
    </MudCard>
</MudContainer>

@code {
    private LoginModel model = new();
    private bool isProcessing = false;
    
    private async Task ProcessLogin()
    {
        isProcessing = true;
        
        try
        {
            var result = await AuthService.LoginAsync(model);
            
            if (result.RequiresMfa)
            {
                NavigationManager.NavigateTo("/mfa");
            }
            else if (result.Succeeded)
            {
                NavigationManager.NavigateTo("/");
            }
            else
            {
                // Mostra errore
            }
        }
        finally
        {
            isProcessing = false;
        }
    }
}
```

### Dashboard Admin

```razor
@page "/admin/dashboard"
@attribute [Authorize(Roles = "Admin")]
@inject IDashboardService DashboardService

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">System Dashboard</MudText>
    
    <MudGrid>
        <MudItem xs="12" md="6" lg="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6">Companies</MudText>
                <MudText Typo="Typo.h4">@_stats.TotalCompanies</MudText>
                <MudText Typo="Typo.body2">@_stats.ActiveCompanies active</MudText>
            </MudPaper>
        </MudItem>
        
        <MudItem xs="12" md="6" lg="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6">Users</MudText>
                <MudText Typo="Typo.h4">@_stats.TotalUsers</MudText>
                <MudText Typo="Typo.body2">@_stats.ActiveUsers active</MudText>
            </MudPaper>
        </MudItem>
        
        <MudItem xs="12" md="6" lg="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6">Documents</MudText>
                <MudText Typo="Typo.h4">@_stats.TotalDocuments</MudText>
                <MudText Typo="Typo.body2">@_stats.DocumentsToday today</MudText>
            </MudPaper>
        </MudItem>
        
        <MudItem xs="12" md="6" lg="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6">Queries</MudText>
                <MudText Typo="Typo.h4">@_stats.TotalQueries</MudText>
                <MudText Typo="Typo.body2">@_stats.QueriesPerHour per hour</MudText>
            </MudPaper>
        </MudItem>
    </MudGrid>
    
    <MudGrid Class="mt-4">
        <MudItem xs="12" md="6">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6" Class="mb-2">System Activity</MudText>
                <ActivityChart Data="@_activityData" />
            </MudPaper>
        </MudItem>
        
        <MudItem xs="12" md="6">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6" Class="mb-2">Queue Status</MudText>
                <QueueStatusPanel QueueStats="@_queueStats" />
            </MudPaper>
        </MudItem>
    </MudGrid>
    
    <MudPaper Class="pa-4 mt-4" Elevation="2">
        <MudText Typo="Typo.h6" Class="mb-2">Recent System Logs</MudText>
        <SystemLogsTable Logs="@_recentLogs" />
    </MudPaper>
</MudContainer>

@code {
    private DashboardStats _stats = new();
    private List<ActivityData> _activityData = new();
    private List<QueueStats> _queueStats = new();
    private List<SystemLog> _recentLogs = new();
    
    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }
    
    private async Task LoadDashboardDataAsync()
    {
        _stats = await DashboardService.GetDashboardStatsAsync();
        _activityData = await DashboardService.GetActivityDataAsync();
        _queueStats = await DashboardService.GetQueueStatsAsync();
        _recentLogs = await DashboardService.GetRecentSystemLogsAsync(10);
    }
}
```

### Gestione Collezioni (User)

```razor
@page "/collections"
@attribute [Authorize(Roles = "User,Company,Admin")]
@inject ICollectionService CollectionService
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudGrid>
        <MudItem xs="12" Class="d-flex justify-space-between align-center">
            <MudText Typo="Typo.h4">My Collections</MudText>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" OnClick="OpenCreateDialog">
                Create Collection
            </MudButton>
        </MudItem>
    </MudGrid>
    
    <MudPaper Class="pa-4 mt-4" Elevation="2">
        <MudTable Items="@_collections" Dense="true" Hover="true" Loading="@_loading" LoadingProgressColor="Color.Primary">
            <ToolBarContent>
                <MudTextField @bind-Value="_searchString" Placeholder="Search" Adornment="Adornment.Start" 
                              AdornmentIcon="@Icons.Material.Filled.Search" IconSize="Size.Medium" Class="mt-0"></MudTextField>
            </ToolBarContent>
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Description</MudTh>
                <MudTh>Documents</MudTh>
                <MudTh>Created</MudTh>
                <MudTh>Actions</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel="Name">@context.Name</MudTd>
                <MudTd DataLabel="Description">@context.Description</MudTd>
                <MudTd DataLabel="Documents">@context.DocumentCount</MudTd>
                <MudTd DataLabel="Created">@context.CreatedAt.ToShortDateString()</MudTd>
                <MudTd>
                    <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" OnClick="@(() => OpenEditDialog(context))" />
                    <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error" 
                                  OnClick="@(() => OpenDeleteDialog(context))" />
                    <MudIconButton Icon="@Icons.Material.Filled.Visibility" Size="Size.Small" Color="Color.Info" 
                                  OnClick="@(() => NavigateToDetails(context))" />
                </MudTd>
            </RowTemplate>
            <PagerContent>
                <MudTablePager />
            </PagerContent>
        </MudTable>
    </MudPaper>
</MudContainer>

@code {
    private List<CollectionDto> _collections = new();
    private bool _loading = true;
    private string _searchString = "";
    
    protected override async Task OnInitializedAsync()
    {
        await LoadCollectionsAsync();
    }
    
    private async Task LoadCollectionsAsync()
    {
        _loading = true;
        
        try
        {
            _collections = await CollectionService.GetUserCollectionsAsync();
        }
        finally
        {
            _loading = false;
        }
    }
    
    private async Task OpenCreateDialog()
    {
        var dialog = DialogService.Show<CreateCollectionDialog>("Create Collection");
        var result = await dialog.Result;
        
        if (!result.Cancelled)
        {
            await LoadCollectionsAsync();
            Snackbar.Add("Collection created successfully", Severity.Success);
        }
    }
    
    private async Task OpenEditDialog(CollectionDto collection)
    {
        var parameters = new DialogParameters
        {
            ["Collection"] = collection
        };
        
        var dialog = DialogService.Show<EditCollectionDialog>("Edit Collection", parameters);
        var result = await dialog.Result;
        
        if (!result.Cancelled)
        {
            await LoadCollectionsAsync();
            Snackbar.Add("Collection updated successfully", Severity.Success);
        }
    }
    
    private async Task OpenDeleteDialog(CollectionDto collection)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = $"Are you sure you want to delete the collection '{collection.Name}'? This action cannot be undone."
        };
        
        var dialog = DialogService.Show<ConfirmDialog>("Confirm Delete", parameters);
        var result = await dialog.Result;
        
        if (!result.Cancelled)
        {
            await CollectionService.DeleteCollectionAsync(collection.Id);
            await LoadCollectionsAsync();
            Snackbar.Add("Collection deleted successfully", Severity.Success);
        }
    }
    
    private void NavigateToDetails(CollectionDto collection)
    {
        // Navigazione ai dettagli della collezione
    }
}
```

### Interfaccia Chatbot

```razor
@page "/chat"
@attribute [Authorize(Roles = "User,Company,Admin")]
@inject IChatService ChatService
@inject ICollectionService CollectionService
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-4">
    <MudGrid>
        <MudItem xs="12" md="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.h6" Class="mb-2">Collections</MudText>
                
                <MudList Clickable="true">
                    @foreach (var collection in _collections)
                    {
                        <MudListItem Icon="@Icons.Material.Filled.Folder" 
                                    IconColor="@(IsCollectionSelected(collection.Id) ? Color.Primary : Color.Default)"
                                    OnClick="@(() => ToggleCollection(collection.Id))">
                            <div class="d-flex align-center">
                                <MudCheckBox @bind-Checked="@collection.IsSelected" Color="Color.Primary" 
                                           Class="mr-2"></MudCheckBox>
                                <MudText>@collection.Name</MudText>
                            </div>
                        </MudListItem>
                    }
                </MudList>
                
                <MudDivider Class="my-4" />
                
                <MudText Typo="Typo.h6" Class="mb-2">Conversations</MudText>
                
                <MudList Clickable="true">
                    @foreach (var conversation in _conversations)
                    {
                        <MudListItem Icon="@Icons.Material.Filled.Chat" 
                                    IconColor="@(conversation.Id == _currentConversationId ? Color.Primary : Color.Default)"
                                    OnClick="@(() => LoadConversation(conversation.Id))">
                            <MudText>@conversation.Title</MudText>
                        </MudListItem>
                    }
                </MudList>
                
                <MudButton Variant="Variant.Filled" Color="Color.Primary" FullWidth="true" 
                          OnClick="StartNewConversation" Class="mt-4">
                    New Conversation
                </MudButton>
            </MudPaper>
        </MudItem>
        
        <MudItem xs="12" md="9">
            <MudPaper Class="pa-4" Elevation="2" Style="height: 70vh; display: flex; flex-direction: column;">
                <MudText Typo="Typo.h6" Class="mb-2">@(_currentConversation?.Title ?? "New Conversation")</MudText>
                
                <div class="flex-grow-1 overflow-auto mb-4" style="padding-right: 8px;">
                    @foreach (var message in _messages)
                    {
                        <div class="@(message.Role == "user" ? "d-flex justify-end mb-2" : "d-flex justify-start mb-2")">
                            <MudPaper Class="pa-3" Elevation="0" 
                                     Style="@(message.Role == "user" ? "background-color: #e3f2fd; max-width: 80%;" : "background-color: #f5f5f5; max-width: 80%;")">
                                <MudText>@message.Content</MudText>
                                
                                @if (message.Citations != null && message.Citations.Any())
                                {
                                    <MudDivider Class="my-2" />
                                    <MudText Typo="Typo.caption">Sources:</MudText>
                                    <div class="d-flex flex-wrap gap-1 mt-1">
                                        @foreach (var citation in message.Citations)
                                        {
                                            <MudChip Size="Size.Small" Color="Color.Primary">@citation.DocumentTitle</MudChip>
                                        }
                                    </div>
                                }
                            </MudPaper>
                        </div>
                    }
                    
                    @if (_isProcessing)
                    {
                        <div class="d-flex justify-start mb-2">
                            <MudPaper Class="pa-3" Elevation="0" Style="background-color: #f5f5f5;">
                                <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="my-2" />
                            </MudPaper>
                        </div>
                    }
                </div>
                
                <div>
                    <MudTextField @bind-Value="_currentMessage" Label="Type your message" Variant="Variant.Outlined" 
                                 Lines="3" Immediate="true" Disabled="@_isProcessing"
                                 Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.Send" 
                                 OnAdornmentClick="SendMessage" OnKeyDown="@HandleKeyDown" />
                </div>
            </MudPaper>
        </MudItem>
    </MudGrid>
</MudContainer>

@code {
    private List<CollectionViewModel> _collections = new();
    private List<ConversationViewModel> _conversations = new();
    private ConversationViewModel _currentConversation;
    private string _currentConversationId;
    private List<MessageViewModel> _messages = new();
    private string _currentMessage = "";
    private bool _isProcessing = false;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadCollectionsAsync();
        await LoadConversationsAsync();
    }
    
    private async Task LoadCollectionsAsync()
    {
        var collections = await CollectionService.GetUserCollectionsAsync();
        _collections = collections.Select(c => new CollectionViewModel
        {
            Id = c.Id,
            Name = c.Name,
            IsSelected = false
        }).ToList();
    }
    
    private async Task LoadConversationsAsync()
    {
        _conversations = await ChatService.GetConversationsAsync();
    }
    
    private bool IsCollectionSelected(string collectionId)
    {
        return _collections.FirstOrDefault(c => c.Id == collectionId)?.IsSelected ?? false;
    }
    
    private void ToggleCollection(string collectionId)
    {
        var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
        if (collection != null)
        {
            collection.IsSelected = !collection.IsSelected;
        }
    }
    
    private async Task LoadConversation(string conversationId)
    {
        _currentConversationId = conversationId;
        _currentConversation = _conversations.FirstOrDefault(c => c.Id == conversationId);
        _messages = await ChatService.GetConversationMessagesAsync(conversationId);
    }
    
    private async Task StartNewConversation()
    {
        _currentConversationId = null;
        _currentConversation = null;
        _messages.Clear();
    }
    
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_currentMessage) || _isProcessing)
            return;
            
        var selectedCollections = _collections.Where(c => c.IsSelected).Select(c => c.Id).ToList();
        if (!selectedCollections.Any())
        {
            Snackbar.Add("Please select at least one collection", Severity.Warning);
            return;
        }
        
        var userMessage = new MessageViewModel
        {
            Role = "user",
            Content = _currentMessage,
            Timestamp = DateTime.Now
        };
        
        _messages.Add(userMessage);
        var messageToBeSent = _currentMessage;
        _currentMessage = "";
        _isProcessing = true;
        
        try
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                // Create new conversation
                var result = await ChatService.CreateConversationAsync("New Conversation", selectedCollections);
                _currentConversationId = result.ConversationId;
                _currentConversation = new ConversationViewModel
                {
                    Id = result.ConversationId,
                    Title = "New Conversation"
                };
                
                // Refresh conversations list
                await LoadConversationsAsync();
            }
            
            // Send message
            var response = await ChatService.SendMessageAsync(_currentConversationId, messageToBeSent, selectedCollections);
            
            // Add response to messages
            _messages.Add(new MessageViewModel
            {
                Role = "assistant",
                Content = response.Content,
                Citations = response.Citations,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }
    
    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && args.CtrlKey)
        {
            SendMessage();
        }
    }
    
    public class CollectionViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
    
    public class ConversationViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
    
    public class MessageViewModel
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public List<CitationViewModel> Citations { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class CitationViewModel
    {
        public string DocumentId { get; set; }
        public string DocumentTitle { get; set; }
        public string Excerpt { get; set; }
    }
}
```

## Gestione Stato dell'Applicazione

L'applicazione utilizza un approccio basato su Fluxor per la gestione dello stato, con store separati per le diverse aree funzionali.

### Esempio di Store per Autenticazione

```csharp
// AuthState.cs
public record AuthState
{
    public bool IsAuthenticated { get; init; }
    public string Username { get; init; }
    public string Role { get; init; }
    public string CompanyId { get; init; }
    public List<string> Permissions { get; init; }
}

// AuthFeature.cs
public class AuthFeature : Feature<AuthState>
{
    public override string GetName() => "Auth";
    
    protected override AuthState GetInitialState()
    {
        return new AuthState
        {
            IsAuthenticated = false,
            Username = null,
            Role = null,
            CompanyId = null,
            Permissions = new List<string>()
        };
    }
}

// AuthActions.cs
public record LoginAction(string Username, string Password);
public record LoginSuccessAction(string Username, string Role, string CompanyId, List<string> Permissions);
public record LoginFailureAction(string ErrorMessage);
public record LogoutAction;

// AuthReducers.cs
public static class AuthReducers
{
    [ReducerMethod]
    public static AuthState OnLoginSuccess(AuthState state, LoginSuccessAction action)
    {
        return state with
        {
            IsAuthenticated = true,
            Username = action.Username,
            Role = action.Role,
            CompanyId = action.CompanyId,
            Permissions = action.Permissions
        };
    }
    
    [ReducerMethod]
    public static AuthState OnLogout(AuthState state, LogoutAction action)
    {
        return new AuthState
        {
            IsAuthenticated = false,
            Username = null,
            Role = null,
            CompanyId = null,
            Permissions = new List<string>()
        };
    }
}

// AuthEffects.cs
public class AuthEffects
{
    private readonly IAuthService _authService;
    private readonly NavigationManager _navigationManager;
    
    public AuthEffects(IAuthService authService, NavigationManager navigationManager)
    {
        _authService = authService;
        _navigationManager = navigationManager;
    }
    
    [EffectMethod]
    public async Task HandleLoginAction(LoginAction action, IDispatcher dispatcher)
    {
        try
        {
            var result = await _authService.LoginAsync(new LoginModel
            {
                Username = action.Username,
                Password = action.Password
            });
            
            if (result.Succeeded)
            {
                dispatcher.Dispatch(new LoginSuccessAction(
                    result.Username,
                    result.Role,
                    result.CompanyId,
                    result.Permissions
                ));
                
                _navigationManager.NavigateTo("/");
            }
            else
            {
                dispatcher.Dispatch(new LoginFailureAction(result.ErrorMessage));
            }
        }
        catch (Exception ex)
        {
            dispatcher.Dispatch(new LoginFailureAction(ex.Message));
        }
    }
    
    [EffectMethod]
    public async Task HandleLogoutAction(LogoutAction action, IDispatcher dispatcher)
    {
        await _authService.LogoutAsync();
        _navigationManager.NavigateTo("/login");
    }
}
```

## Considerazioni di Accessibilità

L'interfaccia utente è progettata per essere accessibile secondo gli standard WCAG 2.1 AA, con particolare attenzione a:

1. **Contrasto del colore**: Rapporto di contrasto minimo di 4.5:1 per il testo
2. **Navigazione da tastiera**: Tutti i componenti sono accessibili tramite tastiera
3. **Etichette ARIA**: Utilizzo appropriato di attributi ARIA per screen reader
4. **Testo alternativo**: Immagini con testo alternativo descrittivo
5. **Struttura semantica**: Utilizzo corretto di elementi HTML semantici

## Considerazioni di Performance

Per garantire prestazioni ottimali, l'interfaccia utente implementa:

1. **Lazy Loading**: Caricamento differito di componenti e moduli
2. **Virtualizzazione**: Rendering efficiente di liste lunghe
3. **Debouncing**: Limitazione delle chiamate API durante l'input utente
4. **Caching**: Memorizzazione nella cache dei dati frequentemente utilizzati
5. **Compressione**: Minimizzazione di CSS e JavaScript

## Conclusioni

L'interfaccia utente progettata con MudBlazor offre un'esperienza utente moderna, reattiva e intuitiva, supportando i diversi ruoli e workflow del sistema. L'utilizzo di componenti riutilizzabili, gestione stato centralizzata e best practice di accessibilità e performance garantisce un'applicazione enterprise-grade robusta e scalabile.
