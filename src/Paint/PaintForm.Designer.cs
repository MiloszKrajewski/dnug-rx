using System.Windows.Forms;

namespace Paint
{
	partial class PaintForm
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
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.PublishButton = new System.Windows.Forms.Button();
			this.SubscribeButton = new System.Windows.Forms.Button();
			this.panel = new System.Windows.Forms.Panel();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this.PublishButton);
			this.flowLayoutPanel1.Controls.Add(this.SubscribeButton);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(759, 30);
			this.flowLayoutPanel1.TabIndex = 0;
			// 
			// PublishButton
			// 
			this.PublishButton.Location = new System.Drawing.Point(3, 3);
			this.PublishButton.Name = "PublishButton";
			this.PublishButton.Size = new System.Drawing.Size(75, 23);
			this.PublishButton.TabIndex = 0;
			this.PublishButton.Text = "Publish";
			this.PublishButton.UseVisualStyleBackColor = true;
			// 
			// SubscribeButton
			// 
			this.SubscribeButton.Location = new System.Drawing.Point(84, 3);
			this.SubscribeButton.Name = "SubscribeButton";
			this.SubscribeButton.Size = new System.Drawing.Size(75, 23);
			this.SubscribeButton.TabIndex = 1;
			this.SubscribeButton.Text = "Subscribe";
			this.SubscribeButton.UseVisualStyleBackColor = true;
			// 
			// panel
			// 
			this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel.Location = new System.Drawing.Point(0, 30);
			this.panel.Name = "panel";
			this.panel.Size = new System.Drawing.Size(759, 530);
			this.panel.TabIndex = 1;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(759, 560);
			this.Controls.Add(this.panel);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Name = "PaintForm";
			this.Text = "Form1";
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button PublishButton;
		private System.Windows.Forms.Button SubscribeButton;
		private Panel panel;
	}
}

