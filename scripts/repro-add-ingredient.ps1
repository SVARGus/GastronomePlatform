# repro-add-ingredient.ps1 — точечное воспроизведение DbUpdateConcurrencyException:
# один POST ингредиента в существующее блюдо (созданное сидом «Плов узбекский»).
# Перед запуском: WebAPI перезапущен с включённым SQL-логированием EF.

param(
    [string]$BaseUrl = 'http://localhost:5195/api',
    [string]$Password = 'Gastronome123!',
    [string]$DishId = '91f6e48a-cc9a-4dc6-a3bf-4769802e047a'
)
$ErrorActionPreference = 'Stop'

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null)
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    if ($null -ne $Body) {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 8))
        return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers `
            -ContentType 'application/json; charset=utf-8' -Body $bytes
    }
    return Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $headers
}

$token = (Invoke-Api POST '/auth/login' @{ login = 'anna'; password = $Password }).accessToken

$ingredient = (Invoke-Api GET '/ingredients/search?query=Морковь&limit=1')[0]
$unit = (Invoke-Api GET '/measure-units') | Where-Object { $_.code -eq 'g' }

Write-Host "Блюдо: $DishId; ингредиент: $($ingredient.id); единица: $($unit.id)"

Invoke-Api POST "/dishes/$DishId/recipe/ingredients/catalog" @{
    ingredientId     = $ingredient.id
    ingredientSpecId = $null
    quantity         = 400
    measureUnitId    = $unit.id
    isOptional       = $false
    preparationNote  = 'нарезать соломкой'
} -Token $token

Write-Host 'Успех — ингредиент добавлен (значит, ошибка была разовой).' -ForegroundColor Green
