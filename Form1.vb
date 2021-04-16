Imports System.IO
Imports System.Net
Imports System.Text
Imports Newtonsoft.Json.Linq
Imports System.Web.Script.Serialization
Public Class Form1
    Dim quizes As New List(Of quiz)
    Dim courses As New List(Of course)
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox1.Text = "http://s1.ebalqa.courses/ncentermoodle"

    End Sub
    Dim CookieJar As CookieContainer
    Private Function DoPost(ByVal URL As String, ByVal PostData As String)
        Dim reader As StreamReader

        Dim Request As HttpWebRequest = HttpWebRequest.Create(URL)

        Request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.14) Gecko/20080404 Firefox/2.0.0.14"
        Request.CookieContainer = CookieJar
        Request.AllowAutoRedirect = False
        Request.ContentType = "application/x-www-form-urlencoded"
        Request.Method = "POST"
        Request.ContentLength = PostData.Length

        Dim requestStream As Stream = Request.GetRequestStream()
        Dim postBytes As Byte() = Encoding.ASCII.GetBytes(PostData)

        requestStream.Write(postBytes, 0, postBytes.Length)
        requestStream.Close()

        Dim Response As HttpWebResponse = Request.GetResponse()

        For Each tempCookie In Response.Cookies
            CookieJar.Add(tempCookie)
        Next

        reader = New StreamReader(Response.GetResponseStream())
        Return reader.ReadToEnd()
        Response.Close()
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TextBox1.Text.EndsWith("/") Then TextBox1.Text = TextBox1.Text.Substring(0, TextBox1.Text.Length - 1)

        'Get Login token
        Dim tokenj As String = DoPost(TextBox1.Text & "/login/token.php?username=" & TextBox2.Text & "&password=" & TextBox3.Text & "&service=moodle_mobile_app", "")
        If tokenj.Contains("Invalid login") Then
            MsgBox("Wrong username or password", MsgBoxStyle.Critical)
            Exit Sub
        End If
        Dim tokenjp = JObject.Parse(tokenj)
        Dim token As JToken = tokenjp("token")

        'Get user id
        Dim infoj As String = DoPost(TextBox1.Text & "/webservice/rest/server.php?moodlewsrestformat=json", "wstoken=" & token.ToString & "&wsfunction=core_webservice_get_site_info")
        Dim infojp = JObject.Parse(infoj)
        Dim userid As JToken = infojp("userid")

        'Get user courses
        Dim coursesj As String = DoPost(TextBox1.Text & "/webservice/rest/server.php?moodlewsrestformat=json", "wstoken=" & token.ToString & "&wsfunction=core_enrol_get_users_courses&userid=29466")
        For Each i In New JavaScriptSerializer().Deserialize(Of course())(coursesj)
            courses.Add(i)
        Next

        'Get user quizes
        Dim quizesj As String = DoPost(TextBox1.Text & "/webservice/rest/server.php?moodlewsrestformat=json", "wstoken=" & token.ToString & "&wsfunction=mod_quiz_get_quizzes_by_courses")
        Dim quizjp = Newtonsoft.Json.Linq.JObject.Parse(quizesj)
        For Each jtoken In quizjp("quizzes")
            Try
                Dim quiz As New quiz
                quiz.name = jtoken("name")
                quiz.id = jtoken("id")
                quiz.courseid = jtoken("course")
                Dim modifier As Integer = jtoken("grade").ToString / jtoken("sumgrades").ToString
                quiz.modi = modifier
                quiz.from = jtoken("sumgrades")
                'Get quiz attempt which includes the grade
                Dim markj As String = DoPost(TextBox1.Text & "/webservice/rest/server.php?moodlewsrestformat=json", "wstoken=" & token.ToString & "&wsfunction=mod_quiz_get_user_attempts&quizid=" & quiz.id & "&userid=" & userid.ToString)
                If markj.StartsWith("{""attempts"":[],") Then
                    quiz.grade = "No Attempt"
                End If

                Try
                    quiz.grade = JObject.Parse(markj)("attempts")(0)("sumgrades")

                Catch exx As Exception

                End Try
                quiz.coursename = courses.FindAll(Function(c) c.id = quiz.courseid)(0).shortname

                quizes.Add(quiz)
            Catch ex As Exception
                MsgBox(ex.Message)
                MsgBox(ex.StackTrace)
            End Try
        Next
        For Each q In quizes
            Dim i As New ListViewItem
            i.Text = q.name
            If q.grade = "No Attempt" Then
                i.SubItems.Add("No Attempt")

            Else
                i.SubItems.Add(q.grade & " / " & q.from & " -- " & q.grade * q.modi & " / " & q.from * q.modi)

            End If
            Try
                i.SubItems.Add(q.coursename)

            Catch ex As Exception

            End Try
            ListView1.Items.Add(i)
        Next
        MsgBox("Done", MsgBoxStyle.Information)
    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Process.Start("https://github.com/halearn/instant-moodle-grades-by-botop")
    End Sub
End Class
Class quiz
    Property name As String
    Property from As String
    Property grade As String
    Property id As String
    Property courseid As String
    Property coursename As String
    Property modi As String
End Class
Public Class course
    Public Property id As Integer
    Public Property shortname As String
    Public Property fullname As String
    Public Property enrolledusercount As Integer
    Public Property idnumber As String
    Public Property visible As Integer
    Public Property summary As String
    Public Property summaryformat As Integer
    Public Property format As String
    Public Property showgrades As Boolean
    Public Property lang As String
    Public Property enablecompletion As Boolean
End Class
