<#
 .SYNOPSIS
    Clean resource groups in the subscription

 .DESCRIPTION
    Allows selecting resource groups in a subscription to clean.

 .PARAMETER subscriptionName
    The subscription name.

#>

param(
    [string] $subscriptionName
)

#*******************************************************************************************************
# Script body
#*******************************************************************************************************
$ErrorActionPreference = "Stop"
$script:ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Import-Module Az

$subscriptionDetails = $null
if (![string]::IsNullOrEmpty($script:subscriptionName)) {
    $subscriptionDetails = Get-AzSubscription -SubscriptionName $script:subscriptionName
}


if (!$subscriptionDetails) {
    $subscriptions = Get-AzSubscription | Where-Object { $_.State -eq "Enabled" }
    $subscriptionId = $null
    if ($subscriptions.Count -eq 0) {
        throw "No active subscriptions found - exiting."
    }
    elseif ($subscriptions.Count -eq 1) {
        $subscriptionId = $subscriptions[0].Id
    }
    else {
        Write-Host "Please choose a subscription from list list (using its Index):"
        $script:index = 0
        $subscriptions | Format-Table -AutoSize -Property `
             @{Name="Index"; Expression = {($script:index++)}},`
             @{Name="Subscription"; Expression = {$_.Name}},`
             @{Name="Id"; Expression = {$_.SubscriptionId}}`
        | Out-Host
        while ($true) {
            $option = Read-Host ">"
            try {
                if ([int]$option -ge 1 -and [int]$option -le $subscriptions.Count) {
                    break
                }
            }
            catch {
                Write-Host "Invalid index '$($option)' provided."
            }
            Write-Host "Choose from the list using an index between 1 and $($subscriptions.Count)."
        }
        $subscriptionId = $subscriptions[$option - 1].Id
    }
    $subscriptionDetails = Get-AzSubscription -SubscriptionId $subscriptionId
    if (!$subscriptionDetails) {
        throw "Failed to get details for subscription $($subscriptionId)"
    }
}

# Update context
$script:context = ($subscriptionDetails | Set-AzContext)
$groups = @()

Write-Host
$allGroups = Get-AzResourceGroup -DefaultProfile $script:context
foreach ($rg in $allGroups) {
    $reply = Read-Host -Prompt "Delete $($rg.ResourceGroupName) ? [y/n]"
    if ($reply -match "[yY]") {
        $groups += $rg.ResourceGroupName
    }
}

Write-Host
Write-Host "Deleting:"
Write-Host $groups

foreach ($group in $groups) {
    Write-Host "Deleting $($group)..."
    Remove-AzResourceGroup -Name $group -DefaultProfile $script:context `
        -Force -ErrorAction SilentlyContinue | Out-Null
    Write-Host "... $($group) deleted!"
}
