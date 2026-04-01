$rids = "win-x64", "osx-x64", "linux-x64", "win-arm64", "osx-arm64", "linux-arm64"

foreach ($rid in $rids)
{
	dotnet publish -c Release -r $rid
}