param(
	[Parameter (Mandatory=$true)]
	[string] $jmeterBinDir, 

	[Parameter (Mandatory=$true)]
	[string] $jmxFilesDir
	)

chcp 1251
dotnet build -c Release
dotnet ".\bin\Release\netcoreapp2.2\jtlParser.dll" $jmeterBinDir $jmxFilesDir