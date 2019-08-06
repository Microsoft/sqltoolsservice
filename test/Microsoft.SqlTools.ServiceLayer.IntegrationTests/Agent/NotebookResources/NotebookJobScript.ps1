$JobId =  "$(ESCAPE_SQUOTE(JOBID))"
$StartTime = $(ESCAPE_SQUOTE(STRTTM))
$StartDate = $(ESCAPE_SQUOTE(STRTDT))
$JSONTable = "select * from notebooks.nb_template where job_id = $JobId"
$sqlResult = Invoke-Sqlcmd -Query $JSONTable -Database $TargetDatabase -MaxCharLength 2147483647
function ParseTableToNotebookOutput {
    param (
        [System.Data.DataTable]
        $DataTable,

        [int]
        $CellExecutionCount
    )
    $TableHTMLText = "<table>"
    $TableSchemaFeilds = @()
    $TableHTMLText += "<tr>"
    foreach ($ColumnName in $DataTable.Columns) {
        $TableSchemaFeilds += @(@{name = $ColumnName.toString() })
        $TableHTMLText += "<th>" + $ColumnName.toString() + "</th>"
    }
    $TableHTMLText += "</tr>"
    $TableSchema = @{ }
    $TableSchema["fields"] = $TableSchemaFeilds

    $TableDataRows = @()
    foreach ($Row in $DataTable) {
        $TableDataRow = [ordered]@{ }
        $TableHTMLText += "<tr>"
        $i = 0
        foreach ($Cell in $Row.ItemArray) {
            $TableDataRow[$i.ToString()] = $Cell.toString()
            $TableHTMLText += "<td>" + $Cell.toString() + "</td>"
            $i++
        }
        $TableHTMLText += "</tr>"
        $TableDataRows += $TableDataRow
    }
    $TableDataResource = @{ }
    $TableDataResource["schema"] = $TableSchema
    $TableDataResource["data"] = $TableDataRows
    $TableData = @{ }
    $TableData["application/vnd.dataresource+json"] = $TableDataResource
    $TableData["text/html"] = $TableHTMLText
    $TableOutput = @{ }
    $TableOutput["output_type"] = "execute_result"
    $TableOutput["data"] = $TableData
    $TableOutput["metadata"] = @{ }
    $TableOutput["execution_count"] = $CellExecutionCount
    return $TableOutput
}

function ParseQueryErrorToNotebookOutput {
    param (
        $QueryError
    )
    $ErrorString = "Msg " + $QueryError.Exception.InnerException.Number +
    ", Level " + $QueryError.Exception.InnerException.Class +
    ", State " + $QueryError.Exception.InnerException.State +
    ", Line " + $QueryError.Exception.InnerException.LineNumber +
    "`r`n" + $QueryError.Exception.Message
    
    $ErrorOutput = @{ }
    $ErrorOutput["output_type"] = "error"
    $ErrorOutput["traceback"] = @()
    $ErrorOutput["evalue"] = $ErrorString
    return $ErrorOutput
}

function ParseStringToNotebookOutput {
    param (
        [System.String]
        $InputString
    )
    $StringOutputData = @{ }
    $StringOutputData["text/html"] = $InputString
    $StringOutput = @{ }
    $StringOutput["output_type"] = "display_data"
    $StringOutput["data"] = $StringOutputData
    $StringOutput["metadata"] = @{ }
    return $StringOutput
}

$TemplateNotebook = $sqlResult.notebook
try {
    $TemplateNotebookJsonObject = ConvertFrom-Json -InputObject $TemplateNotebook
}
catch {
    Throw $_.Exception
}

$DatabaseQueryHashTable = @{ }
$DatabaseQueryHashTable["Verbose"] = $true
$DatabaseQueryHashTable["ErrorVariable"] = "SqlQueryError"
$DatabaseQueryHashTable["OutputAs"] = "DataTables"
$CellExcecutionCount = 1

foreach ($NotebookCell in $TemplateNotebookJsonObject.cells) {
    $NotebookCellOutputs = @()
    if ($NotebookCell.cell_type -eq "markdown" -or $NotebookCell.cell_type -eq "raw" -or $NotebookCell.source -eq "") {
        continue;
    }
    $DatabaseQueryHashTable["Query"] = $NotebookCell.source
    $SqlQueryExecutionTime = Measure-Command { $SqlQueryResult = @(Invoke-Sqlcmd @DatabaseQueryHashTable  4>&1) }
    $NotebookCell.execution_count = $CellExcecutionCount++
    $NotebookCellTableOutputs = @()
    if ($SqlQueryResult) {
        foreach ($SQLQueryResultElement in $SqlQueryResult) {
            switch ($SQLQueryResultElement.getType()) {
                System.Management.Automation.VerboseRecord {
                    $NotebookCellOutputs += ParseStringToNotebookOutput($SQLQueryResultElement.Message)
                }
                System.Data.DataTable {
                    $NotebookCellTableOutputs += ParseTableToNotebookOutput $SQLQueryResultElement  $CellExcecutionCount
                }
                Default { }
            }
        }
    }
    if ($SqlQueryError) {
        $NotebookCellOutputs += ParseQueryErrorToNotebookOutput($SqlQueryError)
    }
    if ($SqlQueryExecutionTime) {
        $NotebookCellExcutionTimeString = "Total execution time: " + $SqlQueryExecutionTime.ToString("hh\:mm\:ss\.fff")
        $NotebookCellOutputs += ParseStringToNotebookOutput($NotebookCellExcutionTimeString)
    }
    $NotebookCellOutputs += $NotebookCellTableOutputs
    $NotebookCell.outputs = $NotebookCellOutputs
}

$result = ($TemplateNotebookJsonObject | ConvertTo-Json -Depth 100)
Write-Output $result
$result = $result.Replace("'","''")
$InsertQuery = "INSERT INTO notebooks.nb_materialized (job_id, run_time, run_date, notebook) VALUES ($JobID, $StartTime, $StartDate,'$result')"
$SqlResult = Invoke-Sqlcmd -Query $InsertQuery -Database $TargetDatabase