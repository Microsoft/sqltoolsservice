SET WORKINGDIR=%~dp0
SET REPOROOT=%WORKINGDIR%..\..

REM clean-up results from previous run
RMDIR %WORKINGDIR%reports\ /S /Q
DEL %WORKINGDIR%coverage.xml
MKDIR reports

REM Setup repo base path

REM backup current CSPROJ files
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK

REM switch PDB type to Full since that is required by OpenCover for now
REM we should remove this step on OpenCover supports portable PDB
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj portable full
cscript /nologo ReplaceText.vbs %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj portable full

REM rebuild the SqlToolsService project
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj  %DOTNETCONFIG%
dotnet restore %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj
dotnet build %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj %DOTNETCONFIG% -r win7-x64

REM run the tests through OpenCover and generate a report
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\Microsoft.SqlTools.ServiceLayer.Test.Common.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.Test.Common\Microsoft.SqlTools.ServiceLayer.Test.Common.csproj %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj %DOTNETCONFIG%
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj %DOTNETCONFIG% 
dotnet restore %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj
dotnet build %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG% 

SET TEST_SERVER=localhost
SET SQLTOOLSSERVICE_EXE=%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\bin\Integration\netcoreapp2.0\win7-x64\MicrosoftSqlToolsServiceLayer.exe
SET SERVICECODECOVERAGE=True
SET CODECOVERAGETOOL="%WORKINGDIR%packages\OpenCover.4.6.684\tools\OpenCover.Console.exe"
SET CODECOVERAGEOUTPUT=coverage.xml

dotnet.exe test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG%"

SET SERVICECODECOVERAGE=FALSE

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.TestDriver.Tests\bin\Debug\netcoreapp2.0

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\Microsoft.SqlTools.ServiceLayer.UnitTests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.UnitTests\bin\Debug\netcoreapp2.0

%CODECOVERAGETOOL% -mergeoutput -register:user -target:dotnet.exe -targetargs:"test %REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\Microsoft.SqlTools.ServiceLayer.IntegrationTests.csproj %DOTNETCONFIG%" -oldstyle -filter:"+[Microsoft.SqlTools.*]* -[xunit*]* -[Microsoft.SqlTools.ServiceLayer.Test*]*" -output:coverage.xml -searchdirs:%REPOROOT%\test\Microsoft.SqlTools.ServiceLayer.IntegrationTests\bin\Debug\netcoreapp2.0

REM Generate the report
"%WORKINGDIR%packages\OpenCoverToCoberturaConverter.0.2.4.0\tools\OpenCoverToCoberturaConverter.exe"  -input:coverage.xml -output:outputCobertura.xml -sources:%REPOROOT%\src\Microsoft.SqlTools.ServiceLayer
"%WORKINGDIR%packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe"  "-reports:coverage.xml" "-targetdir:%WORKINGDIR%\reports"

REM restore original project.json
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.ServiceLayer\Microsoft.SqlTools.ServiceLayer.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.Credentials\Microsoft.SqlTools.Credentials.csproj.BAK 
COPY /Y %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj
DEL %REPOROOT%\src\Microsoft.SqlTools.Hosting\Microsoft.SqlTools.Hosting.csproj.BAK 

EXIT
