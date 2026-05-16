# SalesManagementSystem

Готовая WPF-информационная система для управления продажами торговой организации.

## Возможности

- учет товаров: артикул, наименование, категория, цена, остаток, активность;
- учет клиентов: организация, контактное лицо, телефон, email, адрес;
- создание заказов с несколькими позициями;
- транзакционное списание остатков со склада при создании заказа;
- журнал заказов и просмотр состава выбранного заказа;
- панель показателей: товары, клиенты, заказы, выручка, низкие остатки;
- автоматическое создание базы данных и таблиц при запуске;
- отдельные SQL-скрипты и publish-скрипт для развёртывания.

## Требования

- Windows 10/11;
- .NET Desktop Runtime 10 или публикация с установленным .NET SDK 10;
- Microsoft SQL Server или SQL Server Express;
- учетная запись с правом создать базу `SalesManagementDb` либо заранее созданная база по скрипту.

## Подключение к SQL Server

По умолчанию используется строка из `appsettings.json`:

```json
"Server=localhost;Database=SalesManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Ее можно заменить на строку для SQL Server Express:

```json
"Server=.\\SQLEXPRESS;Database=SalesManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Для промышленного запуска также можно задать переменную окружения `SALES_DB_CONNECTION`; она имеет приоритет над `appsettings.json`.

## Запуск из исходников

```powershell
dotnet restore
dotnet run --project .\SalesManagementSystem.csproj
```

## Развёртывание

1. При необходимости выполните SQL-скрипты из папки `Deployment` в SQL Server Management Studio:

```sql
-- Deployment/01_CreateDatabase.sql
-- Deployment/02_SeedData.sql
```

2. Соберите публикуемую версию:

```powershell
.\Deployment\publish.ps1 -Output .\publish
```

3. Скопируйте папку `publish` на рабочее место пользователя.

4. Проверьте строку подключения в `publish\appsettings.json`.

5. Запустите `SalesManagementSystem.exe`.

## Структура проекта

- `Models` - доменные сущности;
- `Data` - подключение, автоинициализация SQL Server, репозиторий;
- `ViewModels` - MVVM-слой и команды;
- `MainWindow.xaml` - WPF-интерфейс;
- `Deployment` - SQL и publish-скрипт.
