# seed-demo.ps1 — наполнение БД демонстрационными данными через HTTP API.
# ИДЕМПОТЕНТЕН: безопасно перезапускать — существующие пользователи, категории,
# ингредиенты, черновики и опубликованные блюда переиспользуются/пропускаются.
#
# Предварительные условия:
#   1. Миграции всех 5 контекстов применены; единицы измерения вставлены SQL-ом.
#   2. WebAPI запущен на http://localhost:5195 (профиль http).
#   3. pgAdmin под рукой — в середине пауза для назначения ролей SQL-ом
#      (при повторном запуске, если роли уже назначены, — просто Enter).
#
# Запуск из корня репозитория:  powershell -ExecutionPolicy Bypass -File scripts/seed-demo.ps1

param(
    [string]$BaseUrl = 'http://localhost:5195/api',
    [string]$Password = 'Gastronome123!'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Net.Http

# ─────────────────────────────── Хелперы ───────────────────────────────

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null)
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    try {
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 8
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
            return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers `
                -ContentType 'application/json; charset=utf-8' -Body $bytes
        }
        return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers
    } catch [System.Net.WebException] {
        # Печатаем тело ошибки (RFC 7807) — иначе причина 400/409 не видна.
        $resp = $_.Exception.Response
        if ($null -ne $resp) {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream(), [System.Text.Encoding]::UTF8)
            Write-Host "`n[$Method $Path] Ответ сервера:`n$($reader.ReadToEnd())" -ForegroundColor Red
        }
        throw
    }
}

function Get-Token([string]$login) {
    (Invoke-Api POST '/auth/login' @{ login = $login; password = $Password }).accessToken
}

function Esc([string]$s) { [uri]::EscapeDataString($s) }

function New-DishPhoto([string]$path, [string]$hex) {
    $bmp = New-Object System.Drawing.Bitmap(600, 600)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $base = [System.Drawing.ColorTranslator]::FromHtml($hex)
    $g.Clear($base)

    # Валидация Media требует файл ≥ 5 КБ, а одноцветный PNG сжимается в 2–3 КБ.
    # «Фактура» из полупрозрачных пятен ломает сжатие и делает заглушку живее.
    $rnd = New-Object System.Random(42)
    for ($i = 0; $i -lt 4000; $i++) {
        $shade = $rnd.Next(-22, 23)
        $r = [Math]::Min(255, [Math]::Max(0, $base.R + $shade))
        $gr = [Math]::Min(255, [Math]::Max(0, $base.G + $shade))
        $b = [Math]::Min(255, [Math]::Max(0, $base.B + $shade))
        $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, $r, $gr, $b))
        $g.FillEllipse($brush, $rnd.Next(600), $rnd.Next(600), $rnd.Next(2, 9), $rnd.Next(2, 9))
        $brush.Dispose()
    }

    $lightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(55, 255, 255, 255))
    $g.FillEllipse($lightBrush, 80, 60, 280, 230)
    $lightBrush.Dispose()
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()

    $size = (Get-Item $path).Length
    if ($size -lt 5KB) { throw "Сгенерированный PNG меньше 5 КБ ($size байт) — увеличьте шум в New-DishPhoto." }
}

# Multipart-загрузка через HttpClient (curl.exe молча падал на кириллических
# путях: ошибка уходила в stderr, скрипт получал пустой ответ и mediaId=null).
function Send-Photo([string]$token, [string]$key, [string]$hex) {
    $tmp = Join-Path $env:TEMP ("gp-seed-{0}.png" -f $key)
    New-DishPhoto $tmp $hex

    $client = New-Object System.Net.Http.HttpClient
    try {
        $client.DefaultRequestHeaders.Authorization =
            New-Object System.Net.Http.Headers.AuthenticationHeaderValue('Bearer', $token)

        $form = New-Object System.Net.Http.MultipartFormDataContent
        $bytes = [System.IO.File]::ReadAllBytes($tmp)
        $fileContent = New-Object System.Net.Http.ByteArrayContent -ArgumentList @(, $bytes)
        $fileContent.Headers.ContentType =
            [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('image/png')
        $form.Add($fileContent, 'file', [System.IO.Path]::GetFileName($tmp))
        $form.Add((New-Object System.Net.Http.StringContent('Dish')), 'intendedEntityType')

        $response = $client.PostAsync("$BaseUrl/media/upload", $form).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if (-not $response.IsSuccessStatusCode) {
            Write-Host "`n[UPLOAD] $([int]$response.StatusCode) $($response.StatusCode): $body" -ForegroundColor Red
            throw 'Загрузка фото не удалась.'
        }

        $mediaId = ($body | ConvertFrom-Json).mediaId
        if (-not $mediaId) { throw "В ответе загрузки нет mediaId: $body" }
        return $mediaId
    } finally {
        $client.Dispose()
    }
}

function Write-Step([string]$msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

# ─────────────────────── 1. Регистрация пользователей ───────────────────────

Write-Step '1/6 Регистрация пользователей'
foreach ($u in @('admin', 'anna', 'marat', 'demo')) {
    try {
        Invoke-Api POST '/auth/register' @{ email = "$u@gastronome.local"; userName = $u; password = $Password; phone = $null } | Out-Null
        Write-Host "  + $u"
    } catch {
        Write-Host "  = $u — уже существует" -ForegroundColor Yellow
    }
}

# ─────────────────── 2. Пауза: роли назначаются SQL-ом ───────────────────

Write-Step '2/6 Назначение ролей (вручную, pgAdmin; если уже назначены — просто Enter)'
Write-Host @'
  INSERT INTO auth."AspNetUserRoles" ("UserId", "RoleId")
  SELECT u."Id", r."Id"
  FROM auth."AspNetUsers" u JOIN auth."AspNetRoles" r ON r."Name" = 'Admin'
  WHERE u."UserName" = 'admin'
  ON CONFLICT DO NOTHING;

  INSERT INTO auth."AspNetUserRoles" ("UserId", "RoleId")
  SELECT u."Id", r."Id"
  FROM auth."AspNetUsers" u JOIN auth."AspNetRoles" r ON r."Name" = 'Chef'
  WHERE u."UserName" IN ('anna', 'marat')
  ON CONFLICT DO NOTHING;
'@
Read-Host 'Роли назначены? Enter для продолжения'

# ───────────────── 3. Справочники (идемпотентно, от админа) ─────────────────

Write-Step '3/6 Категории и ингредиенты'
$adminToken = Get-Token 'admin'

# Категории: переиспользуем существующие корневые из дерева.
$categories = @{}
foreach ($node in (Invoke-Api GET '/categories/tree')) { $categories[$node.name] = $node.id }

$categoryNames = @('Супы', 'Горячее', 'Салаты', 'Десерты', 'Выпечка', 'Завтраки')
$order = 1
foreach ($name in $categoryNames) {
    if ($categories.ContainsKey($name)) {
        Write-Host "  = категория $name"
    } else {
        $created = Invoke-Api POST '/categories' @{ name = $name; parentId = $null; order = $order; iconMediaId = $null } -Token $adminToken
        $categories[$name] = $created.id
        Write-Host "  + категория $name"
    }
    $order++
}

$units = @{}
foreach ($mu in (Invoke-Api GET '/measure-units')) { $units[$mu.code] = $mu.id }
if (-not $units.ContainsKey('g')) { throw 'Единицы измерения не найдены — выполните SQL-шаг с единицами (лог сессии).' }

# name | unit | isLiquid | density | allergen | dietConflicts
$ingredientRows = @(
    @('Рис девзира',            'g',   $false, $null, $null,        'None'),
    @('Баранина',               'g',   $false, $null, $null,        'Vegetarian, Vegan'),
    @('Говядина',               'g',   $false, $null, $null,        'Vegetarian, Vegan'),
    @('Курица',                 'g',   $false, $null, $null,        'Vegetarian, Vegan'),
    @('Бекон',                  'g',   $false, $null, $null,        'Vegetarian, Vegan, Halal, Kosher'),
    @('Креветки',               'g',   $false, $null, 'Shellfish',  'Vegetarian, Vegan'),
    @('Морковь',                'g',   $false, $null, $null,        'None'),
    @('Лук репчатый',           'g',   $false, $null, $null,        'None'),
    @('Картофель',              'g',   $false, $null, $null,        'None'),
    @('Свёкла',                 'g',   $false, $null, $null,        'None'),
    @('Капуста белокочанная',   'g',   $false, $null, $null,        'None'),
    @('Помидоры',               'g',   $false, $null, $null,        'None'),
    @('Мука пшеничная',         'g',   $false, $null, 'Gluten',     'GlutenFree'),
    @('Спагетти',               'g',   $false, $null, 'Gluten',     'GlutenFree'),
    @('Яйцо куриное',           'pcs', $false, $null, 'Eggs',       'Vegan'),
    @('Творог',                 'g',   $false, $null, 'Dairy',      'Vegan, LactoseFree'),
    @('Сыр пармезан',           'g',   $false, $null, 'Dairy',      'Vegan, LactoseFree'),
    @('Кокосовое молоко',       'ml',  $true,  1.0,   $null,        'None'),
    @('Сахар',                  'g',   $false, $null, $null,        'SugarFree'),
    @('Зира',                   'g',   $false, $null, $null,        'None')
)
$ingredients = @{}
foreach ($row in $ingredientRows) {
    $name = $row[0]
    $found = @(Invoke-Api GET "/ingredients/search?query=$(Esc $name)&limit=10") |
        Where-Object { $_.name -eq $name } | Select-Object -First 1
    if ($found) {
        $ingredients[$name] = $found.id
        Write-Host "  = ингредиент $name"
        continue
    }
    $created = Invoke-Api POST '/ingredients' @{
        name               = $name
        pluralName         = $null
        description        = $null
        imageMediaId       = $null
        isLiquid           = $row[2]
        densityApprox      = $row[3]
        isAllergen         = ($null -ne $row[4])
        allergenType       = $row[4]
        dietConflictsMask  = $row[5]
        baseMeasureUnitId  = $units[$row[1]]
        defaultNutritionId = $null
    } -Token $adminToken
    $ingredients[$name] = $created.id
    Write-Host "  + ингредиент $name"
}

# ─────────────────────────── 4. Блюда с рецептами ───────────────────────────

Write-Step '4/6 Блюда, рецепты, публикация'

$dishes = @(
    @{
        Chef = 'anna'; Name = 'Плов узбекский'; Difficulty = 'Medium'; Cost = 'Moderate'
        Short = 'Классический ферганский плов в казане'
        Desc = 'Рассыпчатый плов с бараниной, жёлтой морковью и зирой. Готовится в казане, как на семейных праздниках в Фергане.'
        Diet = 'Halal'; Photo = '#DF7A42'; Cat = @('Горячее'); Tags = @('Восточная', 'Праздничное'); Views = 57
        History = 'Плов пришёл в Среднюю Азию задолго до появления современных границ; умение сварить настоящий плов до сих пор считается признаком хорошего хозяина.'
        Servings = 6; Intro = 'Главное в плове — не торопиться и дать рису отдохнуть под крышкой.'
        Tips = 'Замачивайте рис заранее — плов получится рассыпчатым.'
        Serve = 'Подавайте горкой на широком блюде, сверху выложите мясо.'
        Ing = @(
            @('Рис девзира', 600, 'g', $null, $false),
            @('Баранина', 500, 'g', $null, $false),
            @('Морковь', 400, 'g', 'нарезать соломкой', $false),
            @('Лук репчатый', 200, 'g', $null, $false),
            @('Зира', 1, 'tsp', 'растереть в ступке', $false)
        )
        Freeform = @(, @('Нут замоченный', 100, 'g', $true))
        Steps = @(
            @('Подготовьте продукты', 'Промойте рис до прозрачной воды и замочите в тёплой подсолённой воде на час. Нарежьте мясо крупными кусками.', $null, $null),
            @('Обжарьте зирвак', 'Раскалите масло в казане, обжарьте лук до золотистого цвета, добавьте мясо и морковь.', $null, $null),
            @('Томите зирвак', 'Добавьте зиру, соль и залейте горячей водой. Томите на среднем огне до мягкости моркови.', 180, 40),
            @('Всыпьте рис', 'Выложите рис ровным слоем и долейте воды на полтора сантиметра выше риса.', $null, $null),
            @('Доведите под крышкой', 'Соберите рис горкой, накройте крышкой и оставьте на слабом огне на двадцать минут.', $null, 20)
        )
        Timing = @(20, 60, 10); Yield = @(2400, 6, 400)
        Nutrition = @(210, 8, 9, 24)
    },
    @{
        Chef = 'anna'; Name = 'Борщ украинский'; Difficulty = 'Medium'; Cost = 'Moderate'
        Short = 'Наваристый борщ с говядиной и свёклой'
        Desc = 'Домашний борщ на говяжьем бульоне со свёклой, капустой и картофелем. Вкуснее всего — на следующий день.'
        Diet = $null; Photo = '#B03A2E'; Cat = @('Супы'); Tags = @('Домашнее'); Views = 60
        History = $null
        Servings = 8; Intro = 'Настоящий борщ должен настояться — сварите его заранее.'
        Tips = 'Свёклу тушите отдельно с ложкой уксуса — цвет останется ярким.'
        Serve = 'Со сметаной, зеленью и чесночными пампушками.'
        Ing = @(
            @('Говядина', 600, 'g', 'на кости', $false),
            @('Свёкла', 300, 'g', $null, $false),
            @('Капуста белокочанная', 300, 'g', $null, $false),
            @('Картофель', 400, 'g', $null, $false),
            @('Морковь', 150, 'g', $null, $false),
            @('Лук репчатый', 150, 'g', $null, $false)
        )
        Freeform = @()
        Steps = @(
            @('Сварите бульон', 'Залейте говядину холодной водой и варите полтора часа, снимая пену.', $null, 90),
            @('Подготовьте овощи', 'Свёклу натрите и потушите отдельно; морковь и лук спассеруйте.', $null, $null),
            @('Соберите борщ', 'В бульон положите картофель и капусту, через десять минут — заправку и свёклу.', $null, 15),
            @('Дайте настояться', 'Снимите с огня и дайте борщу постоять под крышкой минимум полчаса.', $null, 30)
        )
        Timing = @(30, 120, 30); Yield = @(3500, 8, 430)
        Nutrition = @(65, 4, 3, 6)
    },
    @{
        Chef = 'marat'; Name = 'Паста карбонара'; Difficulty = 'Easy'; Cost = 'Moderate'
        Short = 'Римская классика: бекон, яйцо и пармезан'
        Desc = 'Настоящая карбонара без сливок: соус из желтков и тёртого сыра, обжаренный бекон и свежемолотый перец.'
        Diet = $null; Photo = '#E8B95C'; Cat = @('Горячее'); Tags = @('Итальянская', 'Быстро'); Views = 45
        History = $null
        Servings = 2; Intro = 'Весь секрет — снять сковороду с огня до того, как добавите яйца.'
        Tips = 'Оставьте чашку воды от варки пасты — ею удобно регулировать густоту соуса.'
        Serve = 'Сразу с огня, с дополнительным пармезаном и перцем.'
        Ing = @(
            @('Спагетти', 200, 'g', $null, $false),
            @('Бекон', 100, 'g', 'нарезать брусочками', $false),
            @('Яйцо куриное', 3, 'pcs', 'только желтки', $false),
            @('Сыр пармезан', 50, 'g', 'мелко натереть', $false)
        )
        Freeform = @()
        Steps = @(
            @('Отварите пасту', 'Варите спагетти в подсолённой воде на минуту меньше, чем указано на упаковке.', $null, 9),
            @('Обжарьте бекон', 'На сухой сковороде вытопите жир из бекона до хрустящей корочки.', $null, 5),
            @('Соедините', 'Снимите сковороду с огня, добавьте пасту, желтки с сыром и немного воды от варки. Быстро перемешайте.', $null, $null)
        )
        Timing = @(10, 15, $null); Yield = @(500, 2, 250)
        Nutrition = @(380, 15, 18, 38)
    },
    @{
        Chef = 'anna'; Name = 'Сырники'; Difficulty = 'Easy'; Cost = 'Budget'
        Short = 'Пышные сырники из творога'
        Desc = 'Классические сырники на завтрак: минимум муки, максимум творога. С румяной корочкой и нежной серединой.'
        Diet = 'Vegetarian'; Photo = '#E9C271'; Cat = @('Завтраки', 'Десерты'); Tags = @('Завтрак'); Views = 30
        History = $null
        Servings = 3; Intro = 'Чем суше творог, тем меньше муки понадобится.'
        Tips = 'Не делайте сильный огонь — сырники должны пропечься внутри.'
        Serve = 'Со сметаной, мёдом или домашним вареньем.'
        Ing = @(
            @('Творог', 400, 'g', 'отжать', $false),
            @('Яйцо куриное', 1, 'pcs', $null, $false),
            @('Мука пшеничная', 60, 'g', $null, $false),
            @('Сахар', 40, 'g', $null, $false)
        )
        Freeform = @()
        Steps = @(
            @('Замесите тесто', 'Разомните творог с яйцом и сахаром, добавьте муку и перемешайте до однородности.', $null, $null),
            @('Сформируйте', 'Слепите шайбочки, обваляйте в муке и уберите на десять минут в холодильник.', $null, 10),
            @('Обжарьте', 'Жарьте на среднем огне до румяной корочки с двух сторон.', $null, 8)
        )
        Timing = @(15, 15, 10); Yield = @(550, 3, 180)
        Nutrition = @(220, 16, 9, 18)
    },
    @{
        Chef = 'marat'; Name = 'Хинкали'; Difficulty = 'Hard'; Cost = 'Moderate'
        Short = 'Грузинские хинкали с сочной начинкой'
        Desc = 'Тонкое тесто, пряная мясная начинка и главный секрет — бульон внутри. Едят руками, держа за хвостик.'
        Diet = $null; Photo = '#C9A55A'; Cat = @('Горячее'); Tags = @('Восточная'); Views = 18
        History = 'Хинкали родом из горных районов Грузии — Пшави и Хевсурети; складки теста по преданию символизируют солнце.'
        Servings = 4; Intro = 'Терпение при лепке — половина успеха: складок должно быть не меньше девятнадцати.'
        Tips = 'В начинку добавьте ледяную воду — при варке получится больше бульона.'
        Serve = 'Посыпьте свежемолотым чёрным перцем; вилку отложите.'
        Ing = @(
            @('Мука пшеничная', 500, 'g', $null, $false),
            @('Говядина', 350, 'g', 'фарш', $false),
            @('Баранина', 150, 'g', 'фарш', $false),
            @('Лук репчатый', 150, 'g', 'мелко нарубить', $false)
        )
        Freeform = @(, @('Кинза свежая', 20, 'g', $true))
        Steps = @(
            @('Замесите тесто', 'Из муки, воды и соли замесите тугое тесто и дайте ему отдохнуть полчаса.', $null, 30),
            @('Приготовьте начинку', 'Смешайте фарш с луком, специями и ледяной водой до вязкости.', $null, $null),
            @('Слепите хинкали', 'Раскатайте кружки, выложите начинку и соберите складками в мешочек.', $null, $null),
            @('Отварите', 'Варите в кипящей подсолённой воде, пока хинкали не всплывут, плюс ещё пять минут.', $null, 12)
        )
        Timing = @(60, 30, 30); Yield = @(1200, 4, 300)
        Nutrition = $null
    },
    @{
        Chef = 'marat'; Name = 'Том-ям'; Difficulty = 'Medium'; Cost = 'Expensive'
        Short = 'Острый тайский суп на кокосовом молоке'
        Desc = 'Огненный суп с креветками, кокосовым молоком, лемонграссом и лаймом. Баланс острого, кислого и сладкого.'
        Diet = 'LactoseFree'; Photo = '#CE6B3B'; Cat = @('Супы'); Tags = @('Острое', 'Восточная'); Views = 25
        History = $null
        Servings = 4; Intro = 'Не бойтесь остроты — её всегда можно смягчить кокосовым молоком.'
        Tips = 'Креветки добавляйте в самом конце, иначе станут резиновыми.'
        Serve = 'С отварным жасминовым рисом и долькой лайма.'
        Ing = @(
            @('Креветки', 300, 'g', 'очистить', $false),
            @('Кокосовое молоко', 400, 'ml', $null, $false),
            @('Помидоры', 200, 'g', 'черри, половинками', $false),
            @('Лук репчатый', 100, 'g', $null, $false)
        )
        Freeform = @(
            @('Паста том-ям', 3, 'tbsp', $false),
            @('Лемонграсс', 2, 'pcs', $true)
        )
        Steps = @(
            @('Сварите основу', 'Вскипятите воду с пастой том-ям и лемонграссом, дайте покипеть пять минут.', $null, 5),
            @('Добавьте овощи', 'Положите помидоры и лук, влейте кокосовое молоко и доведите до кипения.', $null, 7),
            @('Положите креветки', 'Добавьте креветки и варите не дольше трёх минут. Снимите с огня, добавьте сок лайма.', $null, 3)
        )
        Timing = @(15, 20, $null); Yield = @(1600, 4, 400)
        Nutrition = @(95, 7, 6, 4)
    },
    @{
        Chef = 'anna'; Name = 'Шакшука'; Difficulty = 'Easy'; Cost = 'Budget'
        Short = 'Яйца в пряном томатном соусе'
        Desc = 'Ближневосточный завтрак: яйца, томлённые в густом соусе из помидоров, перца и специй. Одна сковорода — и готово.'
        Diet = 'Vegetarian'; Photo = '#C0392B'; Cat = @('Завтраки'); Tags = @('Завтрак', 'Вегетарианское'); Views = 15
        History = $null
        Servings = 2; Intro = 'Соус должен стать густым до того, как разобьёте яйца.'
        Tips = 'Накройте сковороду крышкой — белок схватится, а желток останется жидким.'
        Serve = 'Прямо в сковороде, со свежим хлебом.'
        Ing = @(
            @('Помидоры', 500, 'g', $null, $false),
            @('Яйцо куриное', 4, 'pcs', $null, $false),
            @('Лук репчатый', 100, 'g', $null, $false)
        )
        Freeform = @(, @('Паприка копчёная', 1, 'tsp', $false))
        Steps = @(
            @('Приготовьте соус', 'Обжарьте лук, добавьте помидоры и специи, тушите до густоты.', $null, 15),
            @('Добавьте яйца', 'Сделайте в соусе углубления и разбейте туда яйца. Накройте крышкой.', $null, 6)
        )
        Timing = @(10, 20, $null); Yield = @(700, 2, 350)
        Nutrition = @(110, 7, 6, 7)
    },
    @{
        Chef = 'anna'; Name = 'Оливье'; Difficulty = 'Easy'; Cost = 'Moderate'
        Short = 'Тот самый салат к празднику'
        Desc = 'Домашний оливье с курицей: всё нарезано аккуратными кубиками, заправлено в меру и настояно в холодильнике.'
        Diet = $null; Photo = '#9CBF74'; Cat = @('Салаты'); Tags = @('Праздничное'); Views = 40
        History = 'Салат назван по имени Люсьена Оливье — шеф-повара московского ресторана «Эрмитаж» второй половины XIX века.'
        Servings = 6; Intro = 'Все ингредиенты должны быть полностью остывшими до сборки.'
        Tips = 'Заправляйте салат не раньше, чем за час до подачи.'
        Serve = 'Охлаждённым, с зеленью.'
        Ing = @(
            @('Картофель', 400, 'g', 'отварить в мундире', $false),
            @('Морковь', 200, 'g', 'отварить', $false),
            @('Яйцо куриное', 4, 'pcs', 'сварить вкрутую', $false),
            @('Курица', 300, 'g', 'отварное филе', $false)
        )
        Freeform = @(
            @('Огурцы солёные', 200, 'g', $false),
            @('Горошек консервированный', 150, 'g', $false)
        )
        Steps = @(
            @('Отварите основу', 'Сварите картофель и морковь в мундире, яйца вкрутую, курицу до готовности. Полностью остудите.', $null, 40),
            @('Нарежьте', 'Все ингредиенты нарежьте одинаковыми аккуратными кубиками.', $null, $null),
            @('Соберите салат', 'Смешайте, посолите и заправьте. Уберите в холодильник на час.', $null, 60)
        )
        Timing = @(20, 40, 60); Yield = @(1800, 6, 300)
        Nutrition = $null
    },
    @{
        Chef = 'marat'; Name = 'Лагман'; Difficulty = 'Medium'; Cost = 'Budget'
        Short = 'Густой суп с домашней лапшой'
        Desc = 'Уйгурский лагман: тянутая лапша, говядина и много овощей в наваристом бульоне со специями.'
        Diet = $null; Photo = '#B99B62'; Cat = @('Супы', 'Горячее'); Tags = @('Восточная', 'Острое'); Views = 20
        History = $null
        Servings = 4; Intro = 'Если не готовы тянуть лапшу руками — возьмите готовую, но толстую.'
        Tips = 'Овощи должны остаться чуть хрустящими — не переваривайте.'
        Serve = 'Посыпьте зеленью и подайте с уксусом и острой пастой.'
        Ing = @(
            @('Мука пшеничная', 400, 'g', 'для лапши', $false),
            @('Говядина', 400, 'g', $null, $false),
            @('Лук репчатый', 150, 'g', $null, $false),
            @('Морковь', 150, 'g', $null, $false),
            @('Помидоры', 200, 'g', $null, $false)
        )
        Freeform = @(, @('Перец болгарский', 150, 'g', $false))
        Steps = @(
            @('Приготовьте лапшу', 'Замесите тесто, дайте отдохнуть и вытяните в длинную лапшу.', $null, 40),
            @('Приготовьте ваджу', 'Обжарьте мясо с овощами, добавьте специи и немного бульона, потушите.', $null, 25),
            @('Соберите лагман', 'Отварите лапшу, разложите по пиалам и залейте ваджой с бульоном.', $null, 8)
        )
        Timing = @(40, 40, $null); Yield = @(2000, 4, 500)
        Nutrition = @(150, 9, 6, 16)
    }
)

$chefTokens = @{ anna = (Get-Token 'anna'); marat = (Get-Token 'marat') }

foreach ($d in $dishes) {
    $token = $chefTokens[$d.Chef]

    # Уже опубликовано? Пропускаем целиком.
    $published = Invoke-Api GET "/dishes/search?text=$(Esc $d.Name)&pageSize=5"
    if (@($published.items | Where-Object { $_.name -eq $d.Name }).Count -gt 0) {
        Write-Host "  = $($d.Name) — уже опубликовано, пропуск" -ForegroundColor Yellow
        continue
    }

    # Черновик существует? Достраиваем его, не создавая дубль.
    $existingIngredientIds = @()
    $existingFreeformTexts = @()
    $existingStepTitles = @()
    $hasMainImage = $false

    $drafts = Invoke-Api GET '/dishes/my-drafts?pageSize=25' -Token $token
    $draftHit = @($drafts.items | Where-Object { $_.name -eq $d.Name }) | Select-Object -First 1

    if ($draftHit) {
        $dishId = $draftHit.id
        $hasMainImage = ($null -ne $draftHit.mainImageId)
        Write-Host "  ~ $($d.Name) — найден черновик, достраиваем" -ForegroundColor Yellow
        $existingRecipe = (Invoke-Api GET "/dishes/$dishId/recipe" -Token $token).recipe
        $existingIngredientIds = @($existingRecipe.ingredients | Where-Object { $_.ingredientId } | ForEach-Object { $_.ingredientId })
        $existingFreeformTexts = @($existingRecipe.ingredients | Where-Object { $_.freeformText } | ForEach-Object { $_.freeformText })
        $existingStepTitles = @($existingRecipe.steps | ForEach-Object { $_.title })
    } else {
        Write-Host "  Блюдо: $($d.Name) (автор $($d.Chef))"
        $draft = Invoke-Api POST '/dishes' @{
            name             = $d.Name
            difficultyLevel  = $d.Difficulty
            costEstimate     = $d.Cost
            shortDescription = $d.Short
            description      = $d.Desc
            dietLabelsMask   = $d.Diet
            historyText      = $d.History
        } -Token $token
        $dishId = $draft.id
    }

    if (-not $hasMainImage) {
        # ASCII-ключ имени temp-файла — кириллица в пути ломала multipart-загрузку.
        $photoKey = ($d.Photo -replace '#', '') + '-' + $d.Yield[0]
        $mediaId = Send-Photo $token $photoKey $d.Photo
        Invoke-Api PATCH "/dishes/$dishId/main-image" @{ mainImageId = $mediaId } -Token $token | Out-Null
    }

    Invoke-Api PUT "/dishes/$dishId/recipe" @{
        introductionText   = $d.Intro
        servingsDefault    = $d.Servings
        isAlcoholic        = $false
        authorTips         = $d.Tips
        servingSuggestions = $d.Serve
        notes              = $null
    } -Token $token | Out-Null

    foreach ($ing in $d.Ing) {
        if ($existingIngredientIds -contains $ingredients[$ing[0]]) { continue }
        Invoke-Api POST "/dishes/$dishId/recipe/ingredients/catalog" @{
            ingredientId     = $ingredients[$ing[0]]
            ingredientSpecId = $null
            quantity         = $ing[1]
            measureUnitId    = $units[$ing[2]]
            isOptional       = $ing[4]
            preparationNote  = $ing[3]
        } -Token $token | Out-Null
    }
    foreach ($ff in $d.Freeform) {
        if ($existingFreeformTexts -contains $ff[0]) { continue }
        Invoke-Api POST "/dishes/$dishId/recipe/ingredients/freeform" @{
            freeformText    = $ff[0]
            quantity        = $ff[1]
            measureUnitId   = $units[$ff[2]]
            isOptional      = $ff[3]
            preparationNote = $null
        } -Token $token | Out-Null
    }

    foreach ($s in $d.Steps) {
        if ($existingStepTitles -contains $s[0]) { continue }
        Invoke-Api POST "/dishes/$dishId/recipe/steps" @{
            title              = $s[0]
            description        = $s[1]
            imageMediaId       = $null
            videoUrl           = $null
            temperatureCelsius = $s[2]
            timerMinutes       = $s[3]
        } -Token $token | Out-Null
    }

    Invoke-Api PUT "/dishes/$dishId/recipe/timing" @{
        prepTimeMinutes   = $d.Timing[0]
        cookTimeMinutes   = $d.Timing[1]
        restTimeMinutes   = $d.Timing[2]
        activeTimeMinutes = $null
        totalTimeMinutes  = 0
        isTotalManual     = $false
    } -Token $token | Out-Null

    Invoke-Api PUT "/dishes/$dishId/recipe/yield" @{
        quantityTotal   = $d.Yield[0]
        yieldUnit       = 'Grams'
        servingsCount   = $d.Yield[1]
        gramsPerServing = $d.Yield[2]
    } -Token $token | Out-Null

    if ($null -ne $d.Nutrition) {
        Invoke-Api PUT "/dishes/$dishId/recipe/nutrition" @{
            calcMethod    = 'Per100g'
            calories      = $d.Nutrition[0]
            proteins      = $d.Nutrition[1]
            fats          = $d.Nutrition[2]
            saturatedFats = $null
            carbs         = $d.Nutrition[3]
            sugar         = $null
            fiber         = $null
            salt          = $null
        } -Token $token | Out-Null
    }

    $catIds = @($d.Cat | ForEach-Object { $categories[$_] })
    Invoke-Api PUT "/dishes/$dishId/categories" @{ categoryIds = $catIds } -Token $token | Out-Null
    Invoke-Api PUT "/dishes/$dishId/tags" @{ tagNames = $d.Tags } -Token $token | Out-Null

    Invoke-Api POST "/dishes/$dishId/publish" -Token $token | Out-Null
    Write-Host "    опубликовано" -ForegroundColor Green

    for ($i = 0; $i -lt $d.Views; $i++) {
        try { Invoke-Api POST "/dishes/$dishId/views" | Out-Null } catch { break }
    }
}

# ─────────────── 4b. Верификация тегов (для tags/popular) ───────────────
# Облако популярных тегов (UC-DSH-061) отдаёт только верифицированные
# админом теги — без этого шага чипсы на главной и фильтр тегов пустые.
# VerifyTag идемпотентен (повторный вызов — тоже 204).

Write-Step '4b/6 Верификация тегов'
$allTagNames = $dishes | ForEach-Object { $_.Tags } | Select-Object -Unique
foreach ($tagName in $allTagNames) {
    $found = @(Invoke-Api GET "/tags/search?query=$(Esc $tagName)&limit=10") |
        Where-Object { $_.name -eq $tagName } | Select-Object -First 1
    if (-not $found) {
        Write-Host "  ! тег '$tagName' не найден" -ForegroundColor Yellow
        continue
    }
    if ($found.isVerified) {
        Write-Host "  = $tagName"
        continue
    }
    Invoke-Api POST "/tags/$($found.id)/verify" -Token $adminToken | Out-Null
    Write-Host "  + $tagName верифицирован"
}

# ─────────────────────── 5. Планы подписки и офферы ───────────────────────

Write-Step '5/6 Тарифные планы'
$existingPlans = @(Invoke-Api GET '/subscription-plans')

if (@($existingPlans | Where-Object { $_.publicName -eq 'Шафран' }).Count -gt 0) {
    Write-Host '  = Шафран — уже на витрине' -ForegroundColor Yellow
} else {
    $saffron = (Invoke-Api POST '/subscription-plans' @{
        planKind      = 'Base'
        publicName    = 'Шафран'
        technicalName = 'saffron-base'
        description   = 'Полные рецепты с пошаговыми фото, калькулятор порций, сезонные подборки и особые категории.'
        requiredRole  = $null
    } -Token $adminToken).planId

    Invoke-Api PUT "/subscription-plans/$saffron/grants" @{
        grants = @(
            @{ grant = 'FullRecipes'; quantity = $null },
            @{ grant = 'PortionCalculator'; quantity = $null },
            @{ grant = 'SeasonalRecipes'; quantity = $null },
            @{ grant = 'SpecialCategories'; quantity = $null }
        )
    } -Token $adminToken | Out-Null

    $saffronMonth = (Invoke-Api POST "/subscription-plans/$saffron/prices" @{
        kind = 'Standard'; publicName = 'Месяц'; durationDays = 30; currency = 'RUB'; amount = 299
        compareAtAmount = $null; discountPercent = $null; trialDays = $null
        isRecurring = $true; isPurchasable = $true
        renewsAsPriceId = $null; fallbackPriceId = $null
        availableFrom = $null; availableUntil = $null; internalNotes = $null
    } -Token $adminToken).priceId

    Invoke-Api POST "/subscription-plans/$saffron/prices" @{
        kind = 'Standard'; publicName = 'Год со скидкой 25%'; durationDays = 365; currency = 'RUB'; amount = 2690
        compareAtAmount = 3588; discountPercent = 25; trialDays = $null
        isRecurring = $true; isPurchasable = $true
        renewsAsPriceId = $null; fallbackPriceId = $null
        availableFrom = $null; availableUntil = $null; internalNotes = $null
    } -Token $adminToken | Out-Null

    Invoke-Api POST "/subscription-plans/$saffron/prices" @{
        kind = 'Trial'; publicName = '7 дней бесплатно'; durationDays = $null; currency = 'RUB'; amount = 0
        compareAtAmount = $null; discountPercent = $null; trialDays = 7
        isRecurring = $true; isPurchasable = $true
        renewsAsPriceId = $saffronMonth; fallbackPriceId = $null
        availableFrom = $null; availableUntil = $null; internalNotes = $null
    } -Token $adminToken | Out-Null
    Write-Host '  + Шафран (месяц / год −25% / триал 7 дней)'
}

if (@($existingPlans | Where-Object { $_.publicName -eq 'Трюфель' }).Count -gt 0) {
    Write-Host '  = Трюфель — уже на витрине' -ForegroundColor Yellow
} else {
    $truffle = (Invoke-Api POST '/subscription-plans' @{
        planKind      = 'Base'
        publicName    = 'Трюфель'
        technicalName = 'truffle-base'
        description   = 'Всё из тарифа Шафран плюс продвижение блюд и реклама на витринах платформы. Для профессиональных поваров.'
        requiredRole  = 'Chef'
    } -Token $adminToken).planId

    Invoke-Api PUT "/subscription-plans/$truffle/grants" @{
        grants = @(
            @{ grant = 'FullRecipes'; quantity = $null },
            @{ grant = 'PortionCalculator'; quantity = $null },
            @{ grant = 'SeasonalRecipes'; quantity = $null },
            @{ grant = 'SpecialCategories'; quantity = $null },
            @{ grant = 'PromotionBasic'; quantity = $null },
            @{ grant = 'DashboardAds'; quantity = $null }
        )
    } -Token $adminToken | Out-Null

    Invoke-Api POST "/subscription-plans/$truffle/prices" @{
        kind = 'Standard'; publicName = 'Месяц'; durationDays = 30; currency = 'RUB'; amount = 990
        compareAtAmount = $null; discountPercent = $null; trialDays = $null
        isRecurring = $true; isPurchasable = $true
        renewsAsPriceId = $null; fallbackPriceId = $null
        availableFrom = $null; availableUntil = $null; internalNotes = $null
    } -Token $adminToken | Out-Null
    Write-Host '  + Трюфель (месяц, роль-гейт Chef)'
}

# ─────────────────────────────── 6. Итог ───────────────────────────────

Write-Step '6/6 Проверка'
$check = Invoke-Api GET '/dishes/search?pageSize=25'
Write-Host "Опубликованных блюд в каталоге: $($check.totalCount)"
$plans = @(Invoke-Api GET '/subscription-plans')
Write-Host "Планов на витрине: $($plans.Count)"
Write-Host "`nГотово. Пользователи (пароль один): admin / anna / marat / demo — $Password" -ForegroundColor Green
