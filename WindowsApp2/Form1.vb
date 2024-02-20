
Imports System.Data.Odbc
Imports System.Data.OleDb
Imports System.Globalization

Public Class Form1

    Public koneski As OdbcConnection
    Dim PathFile As String
    Dim PasswordMdb As String
    Dim DSNname As String



    Private Sub MainWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        mdbsFilePath.Text = "C:\kt/hds_fpsystem.mdb"
        mdbPassword.Text = "tjdgustltmxpa"
        odbsDsnText.Text = "ErpConnectionTest"
    End Sub



    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles button_mdb_read.Click

        PathFile = mdbsFilePath.Text
        PasswordMdb = mdbPassword.Text
        DSNname = odbsDsnText.Text
        Dim Path As String = mdbsFilePath.Text
        Dim password As String = mdbPassword.Text
        Dim odbcDsn As String = odbsDsnText.Text


        If String.IsNullOrEmpty(PathFile) Then
            MessageBox.Show("MDB 파일 경로 입력해주세요.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ElseIf String.IsNullOrEmpty(PasswordMdb) Then
            MessageBox.Show("MDB 암호키 입력해주세요.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ElseIf String.IsNullOrEmpty(DSNname) Then
            MessageBox.Show("ODBS DSN 입력해주세요.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        ElseIf IntervalSelected.SelectedIndex = -1 Then
            MessageBox.Show("주기 시간 선택해주세요.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            ConnectToMariadb()
            Dim selectedItem As String = IntervalSelected.SelectedItem.ToString()
            Dim selecteditemInt As Integer
            MessageBox.Show("주기 시간" & selectedItem, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            If Integer.TryParse(selectedItem, selecteditemInt) Then
                ' Conversion successful, selectedItemInt now holds the integer value
                Me.Timer1.Interval = TimeSpan.FromSeconds(selecteditemInt).TotalMilliseconds
                Me.Timer1.Start()
                testText.AppendText("주기 시간 " & selecteditemInt.ToString() & " 로 설정되었습니다." + vbCrLf)
            Else
                ' Conversion failed, handle the error or provide a default value
                testText.AppendText("Conversion failed. Default value set to 0.")
                selecteditemInt = 0 ' Set a default value or handle the error as needed
            End If
            ' Corrected line with added closing parenthesis
            'Me.Timer1.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds
            'Me.Timer1.Interval = TimeSpan.FromSeconds(Integer.TryParse(selectedItem, selecteditemInt)).TotalMilliseconds
            'Me.Timer1.Start()
        End If

    End Sub

    Private Sub Button1_Click_2(sender As Object, e As EventArgs) Handles button_end_work.Click
        Me.Timer1.Stop()
        testText.AppendText("작업 끝났습니다 " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & Environment.NewLine)
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        RetrieveDataAndDisplay()
    End Sub

    Private Sub ConnectToMariadb()
        Dim connectionString As String = "DSN=" & DSNname

        Try
            koneski = New OdbcConnection(connectionString)

            If koneski.State = ConnectionState.Closed Then
                koneski.Open()
                testText.Text = "Connected to MariaDB!" & vbCrLf
            Else
                testText.Text = "Connection is already open." & vbCrLf
            End If
        Catch ex As Exception
            testText.Text = "Error: " & ex.Message
        End Try
    End Sub

    Private Sub RetrieveDataAndDisplay()
        Dim connectionString As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & PathFile & ";Jet OLEDB:Database Password=" & PasswordMdb
        'Dim connectionString As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\Spring/vbtest.mdb;Jet OLEDB:Database Password=tjdgustltmxpa"
        Dim connection As New OleDbConnection(connectionString)
        Try
            ' Open the connection
            connection.Open()
            testText.AppendText("작업 시작되었습니다 " & DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") & Environment.NewLine)
            ' Write your SQL query to select data from the table
            'Dim query As String = "SELECT * FROM UserInfo WHERE ID = 3 and NAME = 'Eric'"
            'Dim query As String = "SELECT * FROM tb_workresult WHERE str_workempNum = '2076' and str_Mode = '출근' and str_ValidationStatus = '성공'"
            'Dim currentDate As Date = New Date(2023, 2, 8)
            Dim currentDate As Date = Date.Today
            Dim formattedDate As String = currentDate.ToString("yyyy-MM-dd")
            Dim query As String = "SELECT * FROM tb_workresult WHERE LEFT(date_today, 10) = #" & formattedDate & "# AND ((str_Mode = '출근' OR str_Mode = '퇴근') AND str_ValidationStatus = '성공') ORDER BY date_today DESC"

            Dim command As New OleDbCommand(query, connection)


            ' Execute the query and read the data
            Dim reader As OleDbDataReader = command.ExecuteReader()

            ' Check if there is data to read
            If reader.HasRows Then
                ' Initialize an empty string to hold the concatenated result
                Dim resultText As String = ""

                ' Read the data and append it to the resultText
                While reader.Read()
                    ' Assuming you have a TextBox named testText on your form
                    Dim koreanCulture As New CultureInfo("ko-KR")
                    Dim inputDateTime As DateTime = DateTime.ParseExact(reader("date_Today").ToString(), "yyyy-MM-dd tt h:mm:ss", koreanCulture)
                    Dim outputDateString As String = inputDateTime.ToString("yyyy-MM-dd HH:mm:ss")

                    Dim checkQuery As String = "SELECT COUNT(*) FROM TB_WORKER_ATTENDANCE WHERE str_workermpNum = ? AND str_accTerminalPlace = ? AND date_Attestation = ? AND str_Mode = ? AND str_workempName = ?"
                    Dim checkCommand As New OdbcCommand(checkQuery, koneski)
                    checkCommand.Parameters.AddWithValue("@str_workermpNum", Convert.ToInt32(reader("str_workempNum")))
                    checkCommand.Parameters.AddWithValue("@str_accTerminalPlace", reader("str_accTerminalPlace").ToString())
                    checkCommand.Parameters.AddWithValue("@date_Attestation", inputDateTime)
                    checkCommand.Parameters.AddWithValue("@str_Mode", reader("str_Mode").ToString())
                    checkCommand.Parameters.AddWithValue("@str_workempName", reader("str_workempName").ToString())
                    Dim existingRecordsCount As Integer = CInt(checkCommand.ExecuteScalar())

                    ' Insert data only if no matching record exists
                    If existingRecordsCount = 0 Then
                        ' ... (your existing code for insertion)
                        Dim insertQuery As String = "INSERT INTO TB_WORKER_ATTENDANCE(str_workermpNum, str_accTerminalPlace, date_Attestation, str_Mode, str_workempName) " &
                                    "VALUES (?, ?, ?, ?, ?)"

                        ' Create a command with parameters
                        Dim commandInsert As New OdbcCommand(insertQuery, koneski)
                        commandInsert.Parameters.AddWithValue("@str_workermpNum", Convert.ToInt32(reader("str_workempNum")))
                        commandInsert.Parameters.AddWithValue("@str_accTerminalPlace", reader("str_accTerminalPlace").ToString())
                        commandInsert.Parameters.AddWithValue("@date_Attestation", inputDateTime)
                        commandInsert.Parameters.AddWithValue("@str_Mode", reader("str_Mode").ToString())
                        commandInsert.Parameters.AddWithValue("@str_workempName", reader("str_workempName").ToString())
                        Try
                            ' Execute the INSERT query
                            commandInsert.ExecuteNonQuery()

                            ' Display a message indicating successful insertion
                            testText.Text &= "Data inserted successfully." & vbCrLf
                        Catch ex As Exception
                            ' Handle any exceptions during data insertion
                            testText.Text &= "Error inserting data: " & ex.Message & vbCrLf
                        End Try
                        ' Display a message indicating successful insertion
                        testText.Text &= outputDateString & " " & reader("str_workempName").ToString() & " " & reader("str_accTerminalPlace").ToString() & " " & reader("str_Mode").ToString() & " " & reader("str_ValidationStatus").ToString() & vbCrLf
                        testText.Text &= "Data inserted successfully." & vbCrLf
                    Else
                        'testText.Text &= "Data already exists. Skipping insertion." & vbCrLf
                    End If


                End While

            Else
                testText.AppendText("No data found." & vbCrLf)
            End If

        Catch ex As Exception
            testText.AppendText("Error: " & ex.Message & vbCrLf)
        Finally
            ' Close the connection after retrieving the data
            connection.Close()
        End Try
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub Label5_Click(sender As Object, e As EventArgs) Handles Label5.Click

    End Sub

    Private Sub Label6_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub mdbsFilePath_TextChanged(sender As Object, e As EventArgs) Handles mdbsFilePath.TextChanged

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles mdbPassword.TextChanged

    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles odbsDsnText.TextChanged

    End Sub
End Class
