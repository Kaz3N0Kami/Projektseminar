<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class DynPlus100
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Start_Simulation = New Button()
        log = New RichTextBox()
        Label1 = New Label()
        Szenario = New ComboBox()
        Label3 = New Label()
        DebuglevelForm = New Label()
        Debuglevel = New ComboBox()
        SaveLog_GUI = New CheckBox()
        ClearScreen = New Button()
        SuspendLayout()
        ' 
        ' Start_Simulation
        ' 
        Start_Simulation.Font = New Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Start_Simulation.ForeColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Start_Simulation.Location = New Point(914, 73)
        Start_Simulation.Name = "Start_Simulation"
        Start_Simulation.Size = New Size(346, 45)
        Start_Simulation.TabIndex = 0
        Start_Simulation.Text = "Starte Simulation"
        Start_Simulation.UseVisualStyleBackColor = True
        ' 
        ' log
        ' 
        log.Location = New Point(30, 126)
        log.Name = "log"
        log.Size = New Size(1230, 643)
        log.TabIndex = 1
        log.Text = ""
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.Location = New Point(30, 18)
        Label1.Name = "Label1"
        Label1.Size = New Size(335, 46)
        Label1.TabIndex = 2
        Label1.Text = "DynPlus .NET V1.00"
        ' 
        ' Szenario
        ' 
        Szenario.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Szenario.FormattingEnabled = True
        Szenario.Items.AddRange(New Object() {"0", "1", "7"})
        Szenario.Location = New Point(146, 75)
        Szenario.Name = "Szenario"
        Szenario.Size = New Size(63, 36)
        Szenario.TabIndex = 3
        Szenario.Text = "1"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label3.Location = New Point(30, 82)
        Label3.Name = "Label3"
        Label3.Size = New Size(93, 28)
        Label3.TabIndex = 5
        Label3.Text = "Szenario"
        ' 
        ' DebuglevelForm
        ' 
        DebuglevelForm.AutoSize = True
        DebuglevelForm.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        DebuglevelForm.Location = New Point(246, 78)
        DebuglevelForm.Name = "DebuglevelForm"
        DebuglevelForm.Size = New Size(119, 28)
        DebuglevelForm.TabIndex = 8
        DebuglevelForm.Text = "Debuglevel"
        ' 
        ' Debuglevel
        ' 
        Debuglevel.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Debuglevel.FormattingEnabled = True
        Debuglevel.Items.AddRange(New Object() {"1", "2", "3", "4", "5"})
        Debuglevel.Location = New Point(409, 75)
        Debuglevel.Name = "Debuglevel"
        Debuglevel.Size = New Size(63, 36)
        Debuglevel.TabIndex = 7
        Debuglevel.Text = "3"
        ' 
        ' SaveLog_GUI
        ' 
        SaveLog_GUI.AutoSize = True
        SaveLog_GUI.Location = New Point(552, 76)
        SaveLog_GUI.Name = "SaveLog_GUI"
        SaveLog_GUI.Size = New Size(87, 24)
        SaveLog_GUI.TabIndex = 9
        SaveLog_GUI.Text = "SaveLog"
        SaveLog_GUI.UseVisualStyleBackColor = True
        ' 
        ' ClearScreen
        ' 
        ClearScreen.Font = New Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ClearScreen.ForeColor = Color.FromArgb(CByte(64), CByte(0), CByte(0))
        ClearScreen.Location = New Point(671, 75)
        ClearScreen.Name = "ClearScreen"
        ClearScreen.Size = New Size(145, 45)
        ClearScreen.TabIndex = 10
        ClearScreen.Text = "Clear Screen"
        ClearScreen.UseVisualStyleBackColor = True
        ' 
        ' DynPlus100
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1308, 781)
        Controls.Add(ClearScreen)
        Controls.Add(SaveLog_GUI)
        Controls.Add(DebuglevelForm)
        Controls.Add(Debuglevel)
        Controls.Add(Label3)
        Controls.Add(Szenario)
        Controls.Add(Label1)
        Controls.Add(log)
        Controls.Add(Start_Simulation)
        Name = "DynPlus100"
        Text = "DynPlusNet_v100"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Start_Simulation As Button
    Friend WithEvents log As RichTextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Szenario As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents DebuglevelForm As Label
    Friend WithEvents Debuglevel As ComboBox
    Friend WithEvents SaveLog_GUI As CheckBox
    Friend WithEvents ClearScreen As Button

End Class
