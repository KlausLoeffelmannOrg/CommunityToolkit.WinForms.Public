﻿using CommunityToolkit.WinForms.FluentUI.Controls;

namespace Chatty.Views.AboutChatty
{
    partial class FrmAboutChatty
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chatView1 = new ChatView();
            aiFollowUpControl1 = new FluentFollowUpControl();
            _mainLayoutPanel = new TableLayoutPanel();
            button1 = new Button();
            fluentDecoratorPanel1 = new CommunityToolkit.WinForms.FluentUI.FluentDecoratorPanel();
            _mainLayoutPanel.SuspendLayout();
            fluentDecoratorPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // chatView1
            // 
            chatView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            chatView1.Location = new Point(3, 3);
            chatView1.Name = "chatView1";
            _mainLayoutPanel.SetRowSpan(chatView1, 2);
            chatView1.Size = new Size(775, 732);
            chatView1.TabIndex = 0;
            // 
            // aiFollowUpControl1
            // 
            aiFollowUpControl1.Dock = DockStyle.Fill;
            aiFollowUpControl1.Location = new Point(16, 15);
            aiFollowUpControl1.Name = "aiFollowUpControl1";
            aiFollowUpControl1.Size = new Size(297, 629);
            aiFollowUpControl1.TabIndex = 1;
            // 
            // _mainLayoutPanel
            // 
            _mainLayoutPanel.ColumnCount = 2;
            _mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            _mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            _mainLayoutPanel.Controls.Add(chatView1, 0, 0);
            _mainLayoutPanel.Controls.Add(button1, 1, 1);
            _mainLayoutPanel.Controls.Add(fluentDecoratorPanel1, 1, 0);
            _mainLayoutPanel.Dock = DockStyle.Fill;
            _mainLayoutPanel.Location = new Point(0, 0);
            _mainLayoutPanel.Name = "_mainLayoutPanel";
            _mainLayoutPanel.RowCount = 2;
            _mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _mainLayoutPanel.RowStyles.Add(new RowStyle());
            _mainLayoutPanel.Size = new Size(1116, 738);
            _mainLayoutPanel.TabIndex = 2;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Right;
            button1.Location = new Point(932, 675);
            button1.Margin = new Padding(10);
            button1.Name = "button1";
            button1.Size = new Size(174, 53);
            button1.TabIndex = 2;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            // 
            // fluentDecoratorPanel1
            // 
            fluentDecoratorPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fluentDecoratorPanel1.BorderThickness = 1;
            fluentDecoratorPanel1.Controls.Add(aiFollowUpControl1);
            fluentDecoratorPanel1.Location = new Point(784, 3);
            fluentDecoratorPanel1.Name = "fluentDecoratorPanel1";
            fluentDecoratorPanel1.Padding = new Padding(15);
            fluentDecoratorPanel1.Size = new Size(329, 659);
            fluentDecoratorPanel1.TabIndex = 3;
            // 
            // FrmAboutChatty
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1116, 738);
            Controls.Add(_mainLayoutPanel);
            Name = "FrmAboutChatty";
            Text = "Learn about C.h.a.t.t.y.";
            _mainLayoutPanel.ResumeLayout(false);
            fluentDecoratorPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ChatView chatView1;
        private FluentFollowUpControl aiFollowUpControl1;
        private TableLayoutPanel _mainLayoutPanel;
        private Button button1;
        private CommunityToolkit.WinForms.FluentUI.FluentDecoratorPanel fluentDecoratorPanel1;
    }
}