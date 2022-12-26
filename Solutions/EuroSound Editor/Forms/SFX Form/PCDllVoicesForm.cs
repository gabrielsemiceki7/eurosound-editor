﻿using PCAudioDLL.AudioClasses;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace sb_editor.Forms
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public partial class PCDllVoicesForm : Form
    {
        private readonly PCAudioDLL.PCAudioDLL audioTool = ((MainForm)Application.OpenForms[nameof(MainForm)]).audioTool;

        //-------------------------------------------------------------------------------------------------------------------------------
        public PCDllVoicesForm()
        {
            InitializeComponent();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void PCDllVoicesForm_Load(object sender, EventArgs e)
        {
            //Print items
            for (int i = 0; i < audioTool.pcOutVoices.VoicesArray.Length; i++)
            {
                dataGridView1.Rows.Add(new string[] { i.ToString(), "", "", "", "", "", "", "", "" });
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void PCDllVoicesForm_Shown(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(FillListView))
            {
                IsBackground = true
            };
            t.Start();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void FillListView()
        {
            while (!Disposing)
            {
                for (int i = 0; i < audioTool.pcOutVoices.VoicesArray.Length; i++)
                {
                    ExWaveOut currentVoice = audioTool.pcOutVoices.VoicesArray[i];
                    try
                    {
                        dataGridView1.Invoke((MethodInvoker)delegate
                        {
                            DataGridViewRow itemToModify = dataGridView1.Rows[i];
                            if (currentVoice == null)
                            {
                                for (int j = 0; j < itemToModify.Cells.Count; j++)
                                {
                                    DataGridViewCellStyle style = new DataGridViewCellStyle
                                    {
                                        BackColor = SystemColors.Window
                                    };
                                    itemToModify.Cells[j].Style = style;
                                }
                            }
                            else
                            {
                                SetItemState(itemToModify, currentVoice.Active, 1);
                                SetItemState(itemToModify, currentVoice.Played, 2);
                                SetItemState(itemToModify, currentVoice.Playing, 3);
                                SetItemState(itemToModify, currentVoice.Looping, 4);
                                SetItemState(itemToModify, currentVoice.Reverb, 5);
                                SetItemState(itemToModify, currentVoice.Stop_, 6);
                                SetItemState(itemToModify, currentVoice.Stopped, 7);
                                SetItemState(itemToModify, currentVoice.Locked, 8);
                            }
                        });
                    }
                    catch
                    {

                    }
                }
                Thread.Sleep(10);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private void SetItemState(DataGridViewRow rowToModify, bool status, int index)
        {
            //Update Boolean
            if (status)
            {
                DataGridViewCellStyle style = new DataGridViewCellStyle
                {
                    BackColor = Color.Blue
                };
                rowToModify.Cells[index].Style = style;
            }
            else
            {
                DataGridViewCellStyle style = new DataGridViewCellStyle
                {
                    BackColor = SystemColors.Window
                };
                rowToModify.Cells[index].Style = style;
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}