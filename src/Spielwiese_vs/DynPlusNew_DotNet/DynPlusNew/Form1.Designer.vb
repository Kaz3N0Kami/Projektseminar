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
        Label2 = New Label()
        Aufschlagstabelle = New ComboBox()
        SuspendLayout()
        ' 
        ' Start_Simulation
        ' 
        Start_Simulation.Font = New Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Start_Simulation.ForeColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Start_Simulation.Location = New Point(800, 55)
        Start_Simulation.Margin = New Padding(3, 2, 3, 2)
        Start_Simulation.Name = "Start_Simulation"
        Start_Simulation.Size = New Size(303, 34)
        Start_Simulation.TabIndex = 0
        Start_Simulation.Text = "Starte Simulation"
        Start_Simulation.UseVisualStyleBackColor = True
        ' 
        ' log
        ' 
        log.Location = New Point(26, 94)
        log.Margin = New Padding(3, 2, 3, 2)
        log.Name = "log"
        log.Size = New Size(1077, 483)
        log.TabIndex = 1
        log.Text = ""
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 19.8000011F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.Location = New Point(26, 14)
        Label1.Name = "Label1"
        Label1.Size = New Size(266, 37)
        Label1.TabIndex = 2
        Label1.Text = "DynPlus .NET V1.00"
        ' 
        ' Szenario
        ' 
        Szenario.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Szenario.FormattingEnabled = True
        Szenario.Items.AddRange(New Object() {"0", "1", "7", "8"})
        Szenario.Location = New Point(108, 57)
        Szenario.Margin = New Padding(3, 2, 3, 2)
        Szenario.Name = "Szenario"
        Szenario.Size = New Size(56, 29)
        Szenario.TabIndex = 3
        Szenario.Text = "1"
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label3.Location = New Point(26, 62)
        Label3.Name = "Label3"
        Label3.Size = New Size(76, 21)
        Label3.TabIndex = 5
        Label3.Text = "Szenario"
        ' 
        ' DebuglevelForm
        ' 
        DebuglevelForm.AutoSize = True
        DebuglevelForm.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        DebuglevelForm.Location = New Point(170, 58)
        DebuglevelForm.Name = "DebuglevelForm"
        DebuglevelForm.Size = New Size(98, 21)
        DebuglevelForm.TabIndex = 8
        DebuglevelForm.Text = "Debuglevel"
        ' 
        ' Debuglevel
        ' 
        Debuglevel.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Debuglevel.FormattingEnabled = True
        Debuglevel.Items.AddRange(New Object() {"1", "2", "3", "4", "5"})
        Debuglevel.Location = New Point(274, 57)
        Debuglevel.Margin = New Padding(3, 2, 3, 2)
        Debuglevel.Name = "Debuglevel"
        Debuglevel.Size = New Size(56, 29)
        Debuglevel.TabIndex = 7
        Debuglevel.Text = "3"
        ' 
        ' SaveLog_GUI
        ' 
        SaveLog_GUI.AutoSize = True
        SaveLog_GUI.Location = New Point(336, 57)
        SaveLog_GUI.Margin = New Padding(3, 2, 3, 2)
        SaveLog_GUI.Name = "SaveLog_GUI"
        SaveLog_GUI.Size = New Size(70, 19)
        SaveLog_GUI.TabIndex = 9
        SaveLog_GUI.Text = "SaveLog"
        SaveLog_GUI.UseVisualStyleBackColor = True
        ' 
        ' ClearScreen
        ' 
        ClearScreen.Font = New Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ClearScreen.ForeColor = Color.FromArgb(CByte(64), CByte(0), CByte(0))
        ClearScreen.Location = New Point(412, 56)
        ClearScreen.Margin = New Padding(3, 2, 3, 2)
        ClearScreen.Name = "ClearScreen"
        ClearScreen.Size = New Size(127, 34)
        ClearScreen.TabIndex = 10
        ClearScreen.Text = "Clear Screen"
        ClearScreen.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(545, 60)
        Label2.Name = "Label2"
        Label2.Size = New Size(146, 21)
        Label2.TabIndex = 12
        Label2.Text = "Aufschalgstabelle"
        ' 
        ' Aufschlagstabelle
        ' 
        Aufschlagstabelle.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Aufschlagstabelle.FormattingEnabled = True
        Aufschlagstabelle.Items.AddRange(New Object() {"1", "2", "3"})
        Aufschlagstabelle.Location = New Point(697, 57)
        Aufschlagstabelle.Margin = New Padding(3, 2, 3, 2)
        Aufschlagstabelle.Name = "Aufschlagstabelle"
        Aufschlagstabelle.Size = New Size(56, 29)
        Aufschlagstabelle.TabIndex = 11
        Aufschlagstabelle.Text = "1"
        ' 
        ' DynPlus100
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1144, 586)
        Controls.Add(Label2)
        Controls.Add(Aufschlagstabelle)
        Controls.Add(ClearScreen)
        Controls.Add(SaveLog_GUI)
        Controls.Add(DebuglevelForm)
        Controls.Add(Debuglevel)
        Controls.Add(Label3)
        Controls.Add(Szenario)
        Controls.Add(Label1)
        Controls.Add(log)
        Controls.Add(Start_Simulation)
        Margin = New Padding(3, 2, 3, 2)
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
    Friend WithEvents Label2 As Label
    Friend WithEvents Aufschlagstabelle As ComboBox

End Class
