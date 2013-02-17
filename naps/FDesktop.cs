using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using NAPS.wia;
using NAPS.twain;
using WIA;

namespace NAPS
{
    public partial class FDesktop : Form
    {
        private SortedList<int,CScannedImage> images;
        public FDesktop()
        {
            InitializeComponent();
            images = new SortedList<int, CScannedImage>();
        }

        private void thumbnailList1_ItemActivate(object sender, EventArgs e)
        {
            FViewer viewer = new FViewer(images[(int)thumbnailList1.SelectedItems[0].Tag].GetBaseImage());
            viewer.ShowDialog();
        }

        private void updateView()
        {
            thumbnailList1.UpdateView(images);
        }

        private void demoScan()
        {
            /*string path = Application.StartupPath + "\\demo";
            string[] files = Directory.GetFiles(path);
            Array.Sort(files);
            foreach (string file in files)
            {
                int next = images.Count > 0 ? images.Keys[images.Count - 1] + 1 : 0;
                Image img = Image.FromFile(file);
                images.Add(next, img);
            }
            updateView();*/
        }

        private void scanWIA(CScanSettings Profile)
        {
            CWIAAPI api;
            try
            {
                api = new CWIAAPI(Profile);
            }
            catch (Exceptions.EScannerNotFound)
            {
                MessageBox.Show("Device not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CScannedImage img = api.GetImage();

            if (img != null)
            {
                int next = images.Count > 0 ? images.Keys[images.Count - 1] + 1 : 0;
                images.Add(next, img);
            }
            thumbnailList1.UpdateImages(images);
            Application.DoEvents();

            if (Profile.Source != CScanSettings.ScanSource.GLASS)
            {
                while (img != null)
                {
                    img = api.GetImage();
                    if (img != null)
                    {
                        int next = images.Count > 0 ? images.Keys[images.Count - 1] + 1 : 0;
                        images.Add(next, img);
                    }
                    thumbnailList1.UpdateImages(images);
                    Application.DoEvents();
                }
            }
        }

        private void scanTWAIN(CScanSettings profile)
        {
            CTWAINAPI twa;
            try
            {
                twa = new CTWAINAPI(profile, this);
            }
            catch (Exceptions.EScannerNotFound)
            {
                MessageBox.Show("Device not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<CScannedImage> scanned = twa.Scan();
            foreach (CScannedImage bmp in scanned)
            {
                int next = images.Count > 0 ? images.Keys[images.Count - 1] + 1 : 0;
                images.Add(next, bmp);
            }
            thumbnailList1.UpdateImages(images);
        }

        private void deleteItems()
        {
            if (thumbnailList1.SelectedItems.Count > 0)
            {
                if (MessageBox.Show(string.Format("Do you really want to delete {0} items?", thumbnailList1.SelectedItems.Count), "Delete", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    foreach (ListViewItem delitem in thumbnailList1.SelectedItems)
                    {
                        int key = (int)delitem.Tag;
                        images[key].Dispose();
                        images.Remove(key);
                    }
                    thumbnailList1.UpdateImages(images);
                }
            }
        }

        private int getImageBefore(int id)
        {
            int ret = 0;
            foreach (int cid in images.Keys)
            {
                if (cid == id)
                    return ret;
                ret = cid;
            }
            return 0;
        }

        private int getImageAfter(int id)
        {
            bool found = false;
            foreach (int cid in images.Keys)
            {
                if (found)
                    return cid;
                if (cid == id)
                    found = true;
            }
            return 0;
        }

        private void moveUp()
        {
            if (thumbnailList1.SelectedItems.Count > 0)
            {
                if (thumbnailList1.SelectedItems[0].Index > 0)
                {
                    foreach (ListViewItem it in thumbnailList1.SelectedItems)
                    {
                        int before = getImageBefore((int)it.Tag);
                        CScannedImage temp = images[before];
                        images[before] = images[(int)it.Tag];
                        images[(int)it.Tag] = temp;
                        thumbnailList1.Items[thumbnailList1.Items.IndexOf(it) - 1].Selected = true;
                        it.Selected = false;
                    }
                    updateView();
                }
            }
        }

        private void moveDown()
        {
            if (thumbnailList1.SelectedItems.Count > 0)
            {
                if (thumbnailList1.SelectedItems[thumbnailList1.SelectedItems.Count - 1].Index < images.Count - 1)
                {

                    for (int i = thumbnailList1.SelectedItems.Count - 1; i >= 0; i--)
                    {
                        ListViewItem it = thumbnailList1.SelectedItems[i];
                        int after = getImageAfter((int)it.Tag);
                        CScannedImage temp = images[after];
                        images[after] = images[(int)it.Tag];
                        images[(int)it.Tag] = temp;
                        thumbnailList1.Items[thumbnailList1.Items.IndexOf(it) + 1].Selected = true;
                        it.Selected = false;
                    }
                    updateView();
                }
            }
        }

        private void rotateLeft()
        {
            if (thumbnailList1.SelectedItems.Count > 0)
            {
                foreach (ListViewItem it in thumbnailList1.SelectedItems)
                { 
                    images[(int)it.Tag].RotateFlip(RotateFlipType.Rotate270FlipNone);
                }
                updateView();
            }
        }

        private void rotateRight()
        {
            if (thumbnailList1.SelectedItems.Count > 0)
            {
                foreach (ListViewItem it in thumbnailList1.SelectedItems)
                {
                    images[(int)it.Tag].RotateFlip(RotateFlipType.Rotate90FlipNone);
                }
                updateView();
            }
        }

        private void thumbnailList1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    deleteItems();
                    break;
                case Keys.Left:
                    if (e.Control)
                        moveUp();
                    break;
                case Keys.Right:
                    if (e.Control)
                        moveDown();
                    break;
            }
        }

        private void exportPDF(string filename)
        {
            FPDFSave pdfdialog = new FPDFSave();
            pdfdialog.Filename = filename;
            pdfdialog.Images = images.Values;
            pdfdialog.ShowDialog(this);
        }

        private void tsScan_Click(object sender, EventArgs e)
        {
            //demoScan();
            //return;

            FChooseProfile prof = new FChooseProfile();
            prof.ShowDialog();

            if (prof.Profile == null)
                return;

            if (prof.Profile.DeviceDriver == CScanSettings.Driver.WIA)
                scanWIA(prof.Profile);
            else
                scanTWAIN(prof.Profile);
        }

        private void tsSavePDF_Click(object sender, EventArgs e)
        {
            if (images.Count > 0)
            {

                SaveFileDialog sd = new SaveFileDialog();
                sd.OverwritePrompt = true;
                sd.AddExtension = true;
                sd.Filter = "PDF document (*.pdf)|*.pdf";

                if (sd.ShowDialog() == DialogResult.OK)
                {
                    exportPDF(sd.FileName);
                }
            }
        }

        private void tsSaveImage_Click(object sender, EventArgs e)
        {
            if (images.Count > 0)
            {
                SaveFileDialog sd = new SaveFileDialog();
                sd.OverwritePrompt = true;
                sd.AddExtension = true;
                sd.Filter = "Bitmap Files (*.bmp)|*.bmp" +
                "|Enhanced Windows MetaFile (*.emf)|*.emf" +
                "|Exchangeable Image File (*.exif)|*.exif" +
                "|Gif Files (*.gif)|*.gif|JPEG Files (*.jpg)|*.jpg" +
                "|PNG Files (*.png)|*.png|TIFF Files (*.tif)|*.tif";
                sd.DefaultExt = "png";
                sd.FilterIndex = 6;

                if (sd.ShowDialog() == DialogResult.OK)
                {
                    int i = 0;

                    if (images.Count == 1)
                    {
                        using (Bitmap baseImage = images.Values[0].GetBaseImage())
                        {
                            baseImage.Save(sd.FileName);
                        }
                        return;
                    }

                    if (sd.FilterIndex == 7)
                    {
                        Image[] imgs = new Image[images.Count];
                        foreach (CScannedImage img in images.Values)
                        {
                            imgs[i] = img.GetBaseImage();
                            i++;
                        }
                        CTiffHelper.SaveMultipage(imgs, sd.FileName);
                        foreach (Bitmap img in imgs)
                        {
                            img.Dispose();
                        }
                        return;
                    }

                    foreach (CScannedImage img in images.Values)
                    {
                        string filename = Path.GetDirectoryName(sd.FileName) + "\\" + Path.GetFileNameWithoutExtension(sd.FileName) + i.ToString().PadLeft(3, '0') + Path.GetExtension(sd.FileName);
                        using (Bitmap baseImage = img.GetBaseImage())
                        {
                            baseImage.Save(filename);
                        }
                        i++;
                    }
                }
            }
        }

        private void tsPDFEmail_Click(object sender, EventArgs e)
        {
            if (images.Count > 0)
            {
                string path = Application.StartupPath + "\\Scan.pdf";
                exportPDF(path);
                MAPI.CMAPI.SendMail(path, "");
                File.Delete(path);
            }
        }

        private void tsMoveUp_Click(object sender, EventArgs e)
        {
            moveUp();
        }

        private void tsMoveDown_Click(object sender, EventArgs e)
        {
            moveDown();
        }

        private void tsRotateLeft_Click(object sender, EventArgs e)
        {
            rotateLeft();
        }

        private void tsRotateRight_Click(object sender, EventArgs e)
        {
            rotateRight();
        }

        private void tsProfiles_Click(object sender, EventArgs e)
        {
            FManageProfiles pmanager = new FManageProfiles();
            pmanager.ShowDialog();
        }

        private void tsAbout_Click(object sender, EventArgs e)
        {
            new FAbout().ShowDialog();
        }

        private void tsExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}