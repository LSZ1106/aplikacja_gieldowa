# Aplikacja giełdowa (Blazor)

Aplikacja webowa napisana w **Blazor Web App (.NET 10)** służąca do analizy technicznej
notowań giełdowych oraz przeprowadzania symulacji strategii inwestycyjnych opartych na
wskaźnikach technicznych. Projekt powstał jako praca licencjacka.

## Spis treści

- [Funkcjonalności](#funkcjonalności)
- [Technologie](#technologie)
- [Architektura](#architektura)
- [Wymagania](#wymagania)
- [Uruchomienie](#uruchomienie)
- [Konfiguracja](#konfiguracja)
- [Format danych CSV](#format-danych-csv)
- [Struktura projektu](#struktura-projektu)

## Funkcjonalności

Aplikacja udostępnia kilka głównych widoków dostępnych z menu nawigacyjnego:

- **Dane historyczne** – przeglądanie i wczytywanie historycznych notowań (OHLCV) z bazy
  danych lub z pliku CSV.
- **Wskaźniki** – prezentacja wartości wskaźników analizy technicznej dla wybranego zbioru
  danych, zarówno w formie ostatniego stanu, jak i tabeli dziennej.
- **Symulacja** – uruchamianie symulacji strategii tradingowej na danych historycznych na
  podstawie sygnałów kupna/sprzedaży generowanych przez wybrane wskaźniki.
- **Prezentacja wyników** – wizualizacja rezultatów przeprowadzonej symulacji.
- **Ustawienia** – konfiguracja parametrów analizy: źródła danych, kapitału początkowego,
  okresów wskaźników, progów sygnałów oraz zestawu aktywnych wskaźników.

### Obsługiwane wskaźniki techniczne

Wskaźniki liczone są przy użyciu biblioteki **Skender.Stock.Indicators**:

- Średnie kroczące (MA / SMA – krótka i długa)
- RSI (Relative Strength Index)
- Oscylator stochastyczny (STOCH %K / %D)
- MACD (linia MACD, linia sygnału, histogram)
- StochRSI (StochRSI + linia sygnału)

Każdy wskaźnik może być skonfigurowany (okresy, wygładzanie, progi kupna/sprzedaży),
a strategia symulacji może łączyć wiele wskaźników z zadanym progiem zgodności sygnałów
(`GroupSignalAgreementPercent`).

## Technologie

- .NET 10
- Blazor Web App (tryby renderowania: Interactive Server oraz Interactive WebAssembly)
- Entity Framework Core 10 + SQL Server
- Skender.Stock.Indicators 2.6.1
- Bootstrap

## Architektura

Projekt wykorzystuje podział na warstwy w katalogu `Features`:

- **Models** (`Features/Models`) – kontrakty i ustawienia analizy (`AnalysisSettings`,
  `AnalysisContracts`).
- **Services** (`Features/Services`) – logika biznesowa:
  - `TechnicalAnalysisService` – obliczanie wartości wskaźników.
  - `TradingSimulationService` – symulacja strategii i generowanie sygnałów.
  - `AnalysisApplicationService` – orkiestracja procesu analizy.
  - `QuoteDataSetService` – zarządzanie zbiorami danych notowań.
  - `IQuoteDataSource` z implementacjami `CsvQuoteDataSource` i `DatabaseQuoteDataSource`
    – abstrakcja źródła danych (CSV lub baza danych).
  - Magazyny stanu: `AnalysisStores`, `SimulationResultsStore`,
    `PresentationPreferencesStore`.
- **Data** (`Data`) – `ApplicationDbContext`, encje (`DataEntities`, `StockQuote`) oraz
  migracje EF Core.
- **Components** (`Components`) – strony i układy interfejsu Blazor.

Przy starcie aplikacji (`Program.cs`) automatycznie wykonywane są migracje bazy danych oraz
zasilanie przykładowymi danymi (`SeedExampleDataAsync`) z pliku `wwwroot/data/quotes.csv`.

## Wymagania

- .NET 10 SDK
- SQL Server (np. LocalDB / SQL Server Express) lub instancja wskazana w connection stringu
- Visual Studio 2026 lub nowszy / VS Code (opcjonalnie)

## Uruchomienie

1. Sklonuj repozytorium:

   ```bash
   git clone https://github.com/LSZ1106/aplikacja_gieldowa.git
   ```

2. Skonfiguruj connection string `DefaultConnection` (zobacz [Konfiguracja](#konfiguracja)).

3. Przejdź do katalogu projektu i uruchom aplikację:

   ```bash
   cd BlazorApp1/BlazorApp1
   dotnet run
   ```

   Migracje bazy danych oraz załadowanie danych przykładowych wykonają się automatycznie
   przy pierwszym uruchomieniu.

4. Otwórz przeglądarkę pod adresem wskazanym w konsoli (zwykle `https://localhost:xxxx`).

## Konfiguracja

Connection string do bazy danych definiowany jest w pliku `appsettings.json` (klucz
`ConnectionStrings:DefaultConnection`). Do przechowywania danych wrażliwych zalecane jest
użycie **User Secrets** (projekt ma skonfigurowane `UserSecretsId`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=AplikacjaGieldowa;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Domyślne parametry analizy (np. kapitał początkowy 10 000, okresy wskaźników, progi
sygnałów) znajdują się w klasie `AnalysisSettings` i mogą być zmieniane w widoku
**Ustawienia**.

## Format danych CSV

Przykładowe dane znajdują się w pliku `wwwroot/data/quotes.csv` i zawierają notowania w
formacie OHLCV (data, otwarcie, maksimum, minimum, zamknięcie, wolumen). Ten sam format
jest wykorzystywany przy imporcie własnych zbiorów danych przez `CsvQuoteDataSource`.

## Struktura projektu

```
BlazorApp1/BlazorApp1/
├── Components/
│   ├── Layout/            # układy (MainLayout, NavMenu, ...)
│   └── Pages/             # strony (Dane historyczne, Wskaźniki, Symulacja, ...)
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DataEntities.cs    # encje: DataEntities, StockQuote
│   └── Migrations/        # migracje EF Core
├── Features/
│   ├── Models/            # AnalysisSettings, AnalysisContracts
│   └── Services/          # logika analizy, symulacji i źródeł danych
├── wwwroot/
│   └── data/quotes.csv    # przykładowe dane notowań
├── Program.cs             # konfiguracja usług i startu aplikacji
└── appsettings.json
```

## Licencja

Projekt akademicki (praca licencjacka). Brak zdefiniowanej licencji open source.