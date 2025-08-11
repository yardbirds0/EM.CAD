using EM.CAD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.GraphicsSystem;

namespace EM.CAD.Demo
{
    public partial class Form1 : Form
    {
        CadControl _cadControl;
        public Form1()
        {
            Configuration.Configure();
            InitializeComponent();
            _cadControl = new CadControl()
            {
                Dock = DockStyle.Fill
            };
            panel1.Controls.Add(_cadControl);
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dg = new OpenFileDialog()
            {
                Filter = "DWG files|*.dwg|DXF files|*.dxf"
            };
            if (dg.ShowDialog() == DialogResult.OK)
            {
                _cadControl.Open(dg.FileName, Teigha.DatabaseServices.FileOpenMode.OpenForReadAndAllShare);
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            if (_cadControl != null)
            {
                _cadControl.Dispose();
                _cadControl = null;
            }
            Configuration.Close();
            base.OnClosed(e);
        }

        private void 动态演示ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeColor(_cadControl.Database);
        }
        private void SetColor(Teigha.DatabaseServices.Entity entity, Color color)
        {
            var tempColor = entity.Color;
            tempColor.Dispose();
            entity.Color = Teigha.Colors.Color.FromColor(color);
        }
        bool _tmp;
        private void ChangeColor(Teigha.DatabaseServices.Database database)
        {
            Color color = _tmp ? Color.FromArgb(192, 0, 192) : Color.Blue;
            _tmp = !_tmp;
            using (var pTable = (Teigha.DatabaseServices.BlockTable)database.BlockTableId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
            {
                foreach (var blockTableRecordId in pTable)
                {
                    using (var blockTableRecord = (Teigha.DatabaseServices.BlockTableRecord)blockTableRecordId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
                    {
                        foreach (var entid in blockTableRecord)
                        {
                            using (var entity = (Teigha.DatabaseServices.Entity)entid.GetObject(Teigha.DatabaseServices.OpenMode.ForWrite))
                            {
                                var blockName = entity.BlockName;
                                var layerName = entity.Layer;
                                if (entity is Teigha.DatabaseServices.BlockReference blockReference)//todo 块引用
                                {
                                    foreach (Teigha.DatabaseServices.ObjectId attributeId in blockReference.AttributeCollection)
                                    {
                                        using (var attribute = (Teigha.DatabaseServices.AttributeReference)attributeId.GetObject(Teigha.DatabaseServices.OpenMode.ForRead))
                                        {
                                            string fieldName = attribute.Tag;
                                            string value = attribute.TextString;
                                            if (fieldName == "唯一ID" && value == "22222")
                                            {
                                                //InsertBlockTableRecord(blockTableRecordId, "0", "属性块2", blockReference.Position, blockReference.ScaleFactors, blockReference.Rotation);
                                                //entity.Erase();//删除实体
                                                entity.Highlight();
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (entity is Teigha.DatabaseServices.Line line)
                                {
                                    switch (blockName)
                                    {
                                        case "4030010":
                                            SetColor(entity, color);
                                            break;
                                    }
                                }
                                else if (entity is Teigha.DatabaseServices.Hatch hatch)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.Circle circle)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.AttributeDefinition attributeDefinition)
                                {
                                }
                                else if (entity is Teigha.DatabaseServices.MText text)
                                {
                                    text.Contents = "123";
                                    var value = text.Text;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RefreshCache();
        }
        /// <summary>
        /// 刷新图纸缓存
        /// </summary>
        public void RefreshCache()
        {
            if (_cadControl.Database == null || _cadControl.HelperDevice == null)
            {
                MessageBox.Show("请添加数据");
                return;
            }
            if (_cadControl.Database.TileMode)
            {
                try
                {
                    using (Teigha.GraphicsSystem.View pView = _cadControl.HelperDevice.ActiveView)
                    {
                        //if (pView.FieldWidth * 2 < (_cadControl.Database.Extmax.X - _cadControl.Database.Extmin.X))
                        {
                            using (var mode = _cadControl.HelperDevice.CreateModel())
                            {
                                mode.Invalidate(InvalidationHint.kInvalidateViewportCache);
                            }
                            //_cadControl.HelperDevice.Model.Invalidate(InvalidationHint.kInvalidateViewportCache);
                            //_cadControl.HelperDevice.Model.Invalidate(InvalidationHint.kInvalidateAll);
                            _cadControl.HelperDevice.Update();
                        }
                    }

                    //Invalidate();
                }
                catch (System.Runtime.InteropServices.SEHException e)
                {
                    MessageBox.Show(string.Format("刷新失败，请重试！，错误信息：{0}", e.ToString()));
                }
            }
            else
            {
                MessageBox.Show("请设置图纸为‘Model’模式！");
            }
        }

        private void button1_Click(object sender, EventArgs e)
            {
            _cadControl.ActivateTextEditor(); // myCadControl 是您的CadControl实例名
            }

        private void 翻译ToolStripMenuItem_Click(object sender, EventArgs e)
            {

            }


        private void 打开ToolStripMenuItem1_Click(object sender, EventArgs e)
            {
            var openFileDialog = new OpenFileDialog
                {
                Filter = "DWG/DXF Files (*.dwg;*.dxf)|*.dwg;*.dxf",
                Title = "选择一个CAD文件进行准备"
                };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            try
                {
                // 1. 将文件加载到一个临时的源数据库
                using (var sourceDb = new Database(false, true))
                    {
                    sourceDb.ReadDwgFile(openFileDialog.FileName, FileOpenMode.OpenForReadAndReadShare, false, "");

                    // 2. 创建一个我们将在内存中操作的目标数据库，并将所有内容克隆过去
                    Database targetDb = sourceDb.Wblock();

                    // 3. 在目标数据库上执行处理
                    ProcessDatabase(targetDb);


                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        private void ProcessDatabase(Database db)
            {
            // 开启一个事务来修改数据库
            using (var tr = db.TransactionManager.StartTransaction())
                {
                // 打开模型空间进行读写
                var modelSpace = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                // (步骤A) 递归炸开所有块，除了填充
                var explodedEntities = ExplodeAll(modelSpace, tr);

                // (步骤B) 识别中文文本并复制偏移
                var newTextEntities = new List<Entity>();
                foreach (Entity ent in explodedEntities)
                    {
                    string textContent = null;
                    if (ent is DBText dbText) textContent = dbText.TextString;
                    if (ent is MText mText) textContent = mText.Text;

                    // 使用正则表达式检查是否包含中文字符
                    if (!string.IsNullOrEmpty(textContent) && Regex.IsMatch(textContent, @"[\u4e00-\u9fa5]"))
                        {
                        var clone = ent.Clone() as Entity;

                        // 计算一个小的偏移向量 (例如，向下偏移文字高度的1.5倍)
                        double offset = ent is DBText t ? t.Height * 1.5 : (ent as MText).Height * 1.5;
                        var transform = Matrix3d.Displacement(new Vector3d(0, -offset, 0));
                        clone.TransformBy(transform);

                        // 修改颜色以便区分
                        clone.ColorIndex = 1; // 红色

                        newTextEntities.Add(clone);
                        }
                    }

                // 将新创建的文本实体添加到模型空间
                foreach (var newEnt in newTextEntities)
                    {
                    modelSpace.AppendEntity(newEnt);
                    tr.AddNewlyCreatedDBObject(newEnt, true);
                    }

                tr.Commit();
                }
            }

        private List<Entity> ExplodeAll(BlockTableRecord owner, Transaction tr)
            {
            var finalEntities = new List<Entity>();
            var entitiesToProcess = new List<Entity>();

            // 初始填充要处理的实体列表
            foreach (ObjectId id in owner)
                {
                entitiesToProcess.Add((Entity)tr.GetObject(id, OpenMode.ForRead));
                }

            // 循环直到没有可以再炸开的块
            while (entitiesToProcess.Count > 0)
                {
                var currentEnt = entitiesToProcess[0];
                entitiesToProcess.RemoveAt(0);

                // 如果是块引用，则炸开它
                if (currentEnt is BlockReference blockRef)
                    {
                    var exploded = new DBObjectCollection();
                    blockRef.Explode(exploded);
                    foreach (DBObject obj in exploded)
                        {
                        // 将炸开后的子实体加回待处理列表
                        entitiesToProcess.Add((Entity)obj);
                        }
                    }
                else
                    {
                    // 如果不是块（或者是我们不想炸开的东西，比如填充），则直接加入最终列表
                    // 您可以在这里添加更多不想被炸开的类型判断
                    finalEntities.Add(currentEnt);
                    }
                }

            // 清理原始的、未炸开的实体（现在它们已经被炸开的子实体替代了）
            foreach (ObjectId id in owner)
                {
                var entToErase = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                entToErase.Erase();
                }
            // 将最终的、完全炸开的实体加回模型空间
            foreach (var ent in finalEntities)
                {
                owner.AppendEntity(ent);
                tr.AddNewlyCreatedDBObject(ent, true);
                }

            return finalEntities;
            }
        }
}
