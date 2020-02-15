using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace _8_sem_seti_lab1
{
    public partial class Form1 : Form
    {
        private static string connString = "Server=localhost" + ";Database=seti"
              + ";port=3306" + ";User Id=root" + ";password=89279876093";
        private ImageList imageList1 = new ImageList();
        List<string> imgsInfo = new List<string>();

        private int current = 0;
        public static MySqlConnection dbConnect(string connString)
        {
            return new MySqlConnection(connString);
        }
        private static string getSiteName (string url)
        {
            return url.Replace("https://", "").Replace("http://", "").Replace("www.", "").Replace("/", ".");
        }
        public static string saveSite(string url, MySqlConnection conn)
        {
            try
            {
                string sql = "insert into sites (url, name) values ('" + url + "','" + getSiteName(url) + "');";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                return "Page saved successfully!";
            }
            catch (MySqlException e) {
                return e.Message;
            }
        }

        
        public static HtmlAgilityPack.HtmlDocument loadHTML(string url, string encoding) {
            try
            {
                WebClient webClient = new WebClient();
                if (encoding == "windows-1251")
                {
                    webClient.Encoding = Encoding.GetEncoding(1251);
                }
                else if (encoding == "UTF32")
                {
                    webClient.Encoding = Encoding.UTF32;
                }
                else if (encoding == "ASCII")
                {
                    webClient.Encoding = Encoding.ASCII;
                }
                else
                {
                    webClient.Encoding = Encoding.UTF8;
                }
                string page = webClient.DownloadString(url);
                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(page);
                return doc;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return null;
        }
        public static string GetImages(string url, MySqlConnection conn, HtmlAgilityPack.HtmlDocument doc)
        {
            List<Tuple<string, string>> imageLinks = new List<Tuple<string, string>>();
            string res = "";

            try { 
                foreach (var link in doc.DocumentNode.SelectNodes("//img"))
                {
                    Tuple<string, string> t = new Tuple<string, string>(link.GetAttributeValue("src", ""), "style=" + link.GetAttributeValue("style", "")
                        + "\nwidth=" + link.GetAttributeValue("width", "N/S") + "\nheight=" + link.GetAttributeValue("height", "N/S") + "\naligh=" +
                        link.GetAttributeValue("align", "N/S") + "\nalt=" + link.GetAttributeValue("alt", "N/S") + "\nborder=" + link.GetAttributeValue("border", "N/S")
                        ); ;
                    imageLinks.Add(t);
                } 
            }
            catch (System.NullReferenceException e)
            {
                return "No imgs found\n";
            }

            foreach (Tuple<string, string> link in imageLinks)
            {
                Random rnd = new Random();
                string[] parse = link.Item1.Split('.');
                string siteName = getSiteName(url);
                string ext = parse.Last();
                string path = "images" + '/' + siteName;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string filename = path + '/' + Convert.ToString(rnd.Next()) + '.' + ext;
                try
                {
                    WebClient webClient = new WebClient();
                    if (!link.Item1.Contains("https://"))
                    {
                        webClient.DownloadFile(url + '/' + link.Item1, filename);
                    }
                    else
                    {
                        webClient.DownloadFile(link.Item1, filename);
                    }
                } 
                catch (WebException e)
                {
                    res += e.Message + '\n';
                }
                try
                {
                    string sql = "insert into images (site_id, location, original_link, styles) values ((select site_id from sites where url = '" + url + "'),'" + filename + "','" + link.Item1 + "','" + link.Item2.Replace("'", "''") + "');";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteScalar();
                }
                catch (MySqlException e)
                {
                    return e.Message;
                }
                res += "Image saved at " + filename + '\n';
            }

            return res;
        }

        public static string GetHEADs(string url, MySqlConnection conn, HtmlAgilityPack.HtmlDocument doc)
        {
            List<string> HEADs = new List<string>();
            string metaKeywords = "";
            try
            {
                foreach (var meta in doc.DocumentNode.SelectNodes("//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='keywords']"))
                {
                    metaKeywords += meta.GetAttributeValue("content", "");
                }
            } catch (System.NullReferenceException e)
            {
                return "Keywords not found\n";
            }
            Console.WriteLine(metaKeywords);
            string[] keywords = metaKeywords.Split(',');
            foreach (string keyword in keywords) {
                try
                {
                    string sql = "insert into keywords (site_id, value) values ((select site_id from sites where url = '" + url + "'),'" + keyword.Replace("'", "''") + "');";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.CommandType = CommandType.Text; 
                    cmd.ExecuteScalar();
                }
                catch (MySqlException e)
                {
                    return e.Message;
                }
            }

            return "Keywords saved\n";
        }

        public static string GetLists(string url, MySqlConnection conn, HtmlAgilityPack.HtmlDocument doc)
        {
            
            List<string> uls = new List<string>();
            List<string> ols = new List<string>();

            List<string> lisFromUls = new List<string>();
            List<string> lisFromOls = new List<string>();
            string lastId = "0";
            string res = "";
            try
            {
                foreach (var ul in doc.DocumentNode.SelectNodes("//ul"))
                {
                    uls.Add(ul.GetAttributeValue("name", "N/S"));
                    try
                    {
                        string sql = "insert into uls_ols (site_id, type, name) values ((select site_id from sites where url = '" + url + "'),'ul', '" + uls.Last().Replace("'", "''") + "');";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteScalar();
                        string sqlLast = "select last_insert_id();";
                        cmd = new MySqlCommand(sqlLast, conn);
                        cmd.CommandType = CommandType.Text;
                        lastId = cmd.ExecuteScalar().ToString();
                        res += "Found and saved ul\n";
                    }
                    catch (MySqlException e)
                    {
                        res += e.Message;
                    }
                    foreach (var li in ul.Descendants().Where(x => x.Name == "li"))
                    {
                        lisFromUls.Add(li.InnerText);
                        try
                        {
                            string sqlInsert = "insert into lis (ul_ol_id, value) values ('" + lastId + "','" + lisFromUls.Last().Replace("'", "''") + "');";
                            MySqlCommand cmd = new MySqlCommand(sqlInsert, conn);
                            cmd.CommandType = CommandType.Text;
                            cmd.ExecuteScalar();
                            res += "Found and saved li form ul\n";
                        }
                        catch (MySqlException e)
                        {
                            return e.Message;
                        }
                    }
                }
                foreach (var ol in doc.DocumentNode.SelectNodes("//ol"))
                {
                    ols.Add(ol.GetAttributeValue("name", "N/S"));
                    try
                    {
                        string sql = "insert into uls_ols (site_id, type, name) values ((select site_id from sites where url = '" + url + "'),'ol', '" + ols.Last().Replace("'", "''") + "');";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteScalar();
                        string sqlLast = "select last_insert_id();";
                        cmd = new MySqlCommand(sqlLast, conn);
                        cmd.CommandType = CommandType.Text;
                        lastId = cmd.ExecuteScalar().ToString();
                        res += "Found and saved ol\n";
                    }
                    catch (MySqlException e)
                    {
                        res += e.Message;
                    }
                    foreach (var li in ol.Descendants().Where(x => x.Name == "li"))
                    {
                        lisFromOls.Add(li.InnerText);
                        try
                        {
                            string sqlInsert = "insert into lis (ul_ol_id, value) values ('" + lastId + "','" + lisFromUls.Last().Replace("'", "''") + "');";
                            MySqlCommand cmd = new MySqlCommand(sqlInsert, conn);
                            cmd.CommandType = CommandType.Text;
                            cmd.ExecuteScalar();
                            res += "Found and saved li form ol\n";
                        }
                        catch (MySqlException e)
                        {
                            return e.Message;
                        }
                    }
                }
            }
            catch (System.NullReferenceException e)
            {
                res += "No lists to find\n";
            }
            return res;
        }

        public delegate void MyDelegate(string str);

        public Form1()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 0;
        }

        public void app1(string encoding)
        {
            BeginInvoke(new MyDelegate(richTextBox1Add), "");
            string url = textBox1.Text;
            MySqlConnection conn = dbConnect(connString);
            try
            {
                conn.Open();
            } 
            catch (MySql.Data.MySqlClient.MySqlException e)
            {
                BeginInvoke(new MyDelegate(richTextBox2Add), "Unsuccessful attempt to connect to the DB: " + e.Message);
                return;
            }
            BeginInvoke(new MyDelegate(richTextBox1Add), saveSite(url, conn) + '\n');
            HtmlAgilityPack.HtmlDocument doc = loadHTML(url, encoding);
            BeginInvoke(new MyDelegate(richTextBox1Add), GetImages(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox1Add), GetHEADs(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox1Add), GetLists(url, conn, doc) + '\n');
            conn.Close();

        }
        public void richTextBox1Add(string str)
        {
            if (str == "")
            {
                richTextBox1.Text = "";
            }
            else
            {
                richTextBox1.Text += str;
            }
        }
        

        public Thread thread1;
        public Thread thread2;
        public Thread thread3;

        private void button1_Click(object sender, EventArgs e)
        {
            string encoding = comboBox1.SelectedItem.ToString();
            thread1 = new Thread(() => app1(encoding));
            thread1.Start();
        }

        public void app2(string encoding)
        {
            BeginInvoke(new MyDelegate(richTextBox2Add), "");
            string url = textBox2.Text;
            MySqlConnection conn = dbConnect(connString);
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                BeginInvoke(new MyDelegate(richTextBox2Add), "Unsuccessful attempt to connect to the DB: " + e.Message);
                return;
            }
            BeginInvoke(new MyDelegate(richTextBox2Add), saveSite(url, conn) + '\n');
            HtmlAgilityPack.HtmlDocument doc = loadHTML(url, encoding);
            BeginInvoke(new MyDelegate(richTextBox2Add), GetImages(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox2Add), GetHEADs(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox2Add), GetLists(url, conn, doc) + '\n');
            conn.Close();

        }
        public void richTextBox2Add(string str)
        {
            if (str == "")
            {
                richTextBox2.Text = "";
            }
            else
            {
                richTextBox2.Text += str;
            }
        }

        public void app3(string encoding)
        {
            BeginInvoke(new MyDelegate(richTextBox3Add), "");
            string url = textBox3.Text;
            MySqlConnection conn = dbConnect(connString);
            try
            {
                conn.Open();
            }
            catch (MySql.Data.MySqlClient.MySqlException e)
            {
                BeginInvoke(new MyDelegate(richTextBox2Add), "Unsuccessful attempt to connect to the DB: " + e.Message);
                return;
            }
            BeginInvoke(new MyDelegate(richTextBox3Add), saveSite(url, conn) + '\n');
            HtmlAgilityPack.HtmlDocument doc = loadHTML(url, encoding);
            BeginInvoke(new MyDelegate(richTextBox3Add), GetImages(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox3Add), GetHEADs(url, conn, doc) + '\n');
            BeginInvoke(new MyDelegate(richTextBox3Add), GetLists(url, conn, doc) + '\n');
            conn.Close();

        }
        public void richTextBox3Add(string str)
        {
            if (str == "")
            {
                richTextBox3.Text = "";
            }
            else
            {
                richTextBox3.Text += str;
            }
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            string encoding = comboBox1.SelectedItem.ToString();
            thread2 = new Thread(() => app2(encoding));
            thread2.Start();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string encoding = comboBox1.SelectedItem.ToString();
            thread3 = new Thread(() => app3(encoding));
            thread3.Start();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            connString = "Server=localhost" + ";Database=" + textBox4.Text
              + ";port=3306" + ";User Id=" + textBox5.Text + ";password=" + textBox6.Text;
            MySqlConnection conn = dbConnect(connString);
            try
            {
                conn.Open();
                conn.Close();
            } 
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            MessageBox.Show("Connection OK");

        }

        private void button5_Click(object sender, EventArgs e)
        {
            MySqlConnection conn  = dbConnect(connString);
            string url = textBox7.Text;
            try
            {
                conn.Open();
                string sql = "select * from keywords inner join sites on keywords.site_id = sites.site_id where url= '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                string res = "";
                MySqlDataReader dr;
                dr = cmd.ExecuteReader();
                int i = 0;
                while (dr.Read())
                {
                    i++;
                    res += i.ToString() + "     " + dr.GetString("value") + '\n';
                }
                richTextBox4.Text = res;
                if (res.Length == 0)
                {
                    richTextBox4.Text = "Nothing to show";
                }
                dr.Close();
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No matching data!");
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = dbConnect(connString);
            string url = textBox7.Text;
            try
            {
                conn.Open();
                string sql = "delete keywords from keywords inner join sites on keywords.site_id = sites.site_id where sites.url= '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;  
                string res = "The keywords have been successfully removed!";
                cmd.ExecuteScalar();
                richTextBox4.Text = res;
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No matching data!");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = dbConnect(connString);
            string url = textBox8.Text;
            string res = "";
            string uls = "";
            try
            {
                conn.Open();
                string sql = "select ul_ol_id, type, uls_ols.name from uls_ols inner join sites on uls_ols.site_id = sites.site_id where url = '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                MySqlDataReader dr;
                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string ul_ol_id = dr.GetString("ul_ol_id");
                    uls += dr.GetString("ul_ol_id") + "      " + dr.GetString("type") + "      " + dr.GetString("name") + '\n';
                }
                dr.Close();

                string[] ulsAr = uls.Split('\n');

                foreach (string ul in ulsAr)
                {
                    res += ul;
                    string ul_ol_id = ul.Split(' ').First();
                    string sqlLis = "select value from lis where ul_ol_id = '" + ul_ol_id + "';";
                    cmd = new MySqlCommand(sqlLis, conn);
                    cmd.CommandType = CommandType.Text;
                    dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        res += "\n----" + dr.GetString("value").Replace("\n", "") + '\n';
                    }
                    dr.Close();

                }
                richTextBox5.Text = res.Replace("\t", "");
                if (res.Length == 0)
                {
                    richTextBox5.Text = "Nothing to show";

                }
                dr.Close();
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("No matching data!");

            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = dbConnect(connString);
            string url = textBox8.Text;
            try
            {
                conn.Open();
                string sql = "delete uls_ols from uls_ols inner join sites on uls_ols.site_id = sites.site_id where url = '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text; 
                cmd = new MySqlCommand(sql, conn);
                string res = "The lists have been successfully removed!";
                cmd.ExecuteScalar();
                richTextBox5.Text = res;
                conn.Close();

            } 
            catch (Exception ex)
            {
                MessageBox.Show("No matching data!");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = dbConnect(connString);
            string url = textBox9.Text;
            imgsInfo = new List<string>();
            imageList1 = new ImageList();
            imageList1.ImageSize = new Size(256, 256);
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            try
            {
                conn.Open();
                string sql = "select location, original_link, styles from images inner join sites on images.site_id = sites.site_id where url = '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                MySqlDataReader dr;
                dr = cmd.ExecuteReader();
                current = 0;
                while (dr.Read())
                {
                    string location = dr.GetString("location");
                    using (FileStream stream = new FileStream(location, FileMode.Open))
                    {
                        imageList1.Images.Add(Image.FromStream(stream));
                    }
                    imgsInfo.Add("Original link: " + dr.GetString("original_link") + "\nSaved at: " + location + "\nOriginal CSS format:\n" + dr.GetString("styles"));
                    label9.Text = (current + 1).ToString() + '/' + imageList1.Images.Count.ToString();
                }
                
                dr.Close();
                pictureBox1.Image = imageList1.Images[current];
                richTextBox6.Text = imgsInfo[current];
            } 
            catch (Exception ex)
            {
                if (imageList1.Images.Count > 0) {
                    pictureBox1.Image = imageList1.Images[current];
                    richTextBox6.Text = imgsInfo[current];
                }
                MessageBox.Show("Failed to open:\n " + ex.Message);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (imageList1.Images.Count > 0)
            {
                current++;
                if (current == imageList1.Images.Count)
                {
                    current = 0;
                }
                pictureBox1.Image = imageList1.Images[current];
                label9.Text = (current + 1).ToString() + '/' + imageList1.Images.Count.ToString();
                richTextBox6.Text = imgsInfo[current];
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (imageList1.Images.Count > 0)
            {
                current--;
                if (current < 0)
                {
                    current = imageList1.Images.Count - 1;
                }
                pictureBox1.Image = imageList1.Images[current];
                label9.Text = (current + 1).ToString() + '/' + imageList1.Images.Count.ToString();
                richTextBox6.Text = imgsInfo[current];
            }
        }
        public static void empty(System.IO.DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);

        }

        private void button10_Click(object sender, EventArgs e)
        {
            MySqlConnection conn = dbConnect(connString);
            string url = textBox9.Text;
            try
            {
                conn.Open();
                string sql = "delete images from images inner join sites on images.site_id = sites.site_id where url = '" + url + "';";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteScalar();
                string siteName = getSiteName(url);
                string path = "images\\" + siteName;
                System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(path);
                empty(directory);
                richTextBox6.Text = "Images removed!";
                conn.Close();
                current = 0;
                imageList1.Images.Clear();
                label9.Text = "0/0";
                pictureBox1.Image = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No matching data!\n" + ex.Message);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (thread1 != null && thread1.IsAlive)
            {
                thread1.Abort();
                richTextBox1.Text = "Thread #1 stopped!";

            } else
            {
                richTextBox1.Text = "Thread #1 is not running!";
            }

        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (thread2 != null && thread2.IsAlive)
            {
                thread2.Abort();
                richTextBox2.Text = "Thread #2 stopped!";
            }
            else
            {
                richTextBox2.Text = "Thread #2 is not running!";
            }

        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (thread3 != null && thread3.IsAlive)
            {
                thread3.Abort();
                richTextBox3.Text = "Thread #3 stopped!";
            }
            else
            {
                richTextBox3.Text = "Thread #3 is not running!";
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (thread1 != null)
            {
                thread1.Abort();
                richTextBox1.Text = "Thread #1 stopped!";
            }
            else
            {
                richTextBox1.Text = "Thread #1 is not running!";
            }
            if (thread2 != null)
            {
                thread2.Abort();
                richTextBox2.Text = "Thread #2 stopped!";
            }
            else
            {
                richTextBox2.Text = "Thread #2 is not running!";
            }
            if (thread3 != null)
            {
                thread3.Abort();
                richTextBox3.Text = "Thread #3 stopped!";
            }
            else
            {
                richTextBox3.Text = "Thread #3 is not running!";
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {
                MySqlConnection conn = dbConnect(connString);
                conn.Open();
                string sql = "drop table if exists images; drop table if exists keywords; drop table if exists lis; drop table if exists uls_ols; drop table if exists sites;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                conn.Close();
                string path = "images";
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                MessageBox.Show("Successfully deleted data storage!");
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = File.ReadAllText(@"migrations.sql");
                sql = sql.Replace("seti", textBox4.Text);

                MySqlConnection conn = dbConnect(connString);
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                conn.Close();
                string path = "images";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                MessageBox.Show("Successfully created data storage!");

            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }

        }
        private void button19_Click(object sender, EventArgs e)
        {
            try
            {
                string url = textBox9.Text;
                string siteName = getSiteName(url);
                string path = "images\\" + siteName;
                Process.Start(path);
            } catch (Exception ex)
            {
                MessageBox.Show("Can't open site's directory!\n" + ex.Message);
                try
                {
                    Process.Start("images");
                } catch (Exception exc)
                {
                    MessageBox.Show("No image folder!\n" + exc.Message);
                }
            }
        }
    }
}
