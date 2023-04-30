using System;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Fiddler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

[assembly: Fiddler.RequiredVersion("5.0.0.0")]
[assembly: AssemblyVersion("0.0.2")]
[assembly: AssemblyTitle("Fiddler Request To Python Code")]
[assembly: AssemblyDescription("将 Fiddler 的抓包请求转换为 Python 代码")]

namespace ToPython
{
    public class ToPython : IFiddlerExtension
    {
        private TabPage _page;
        private RichTextBox codeBox;

        private void TextBox_LinkClicked(object sender, LinkClickedEventArgs e){
            Process.Start(e.LinkText);
        }
        
        private void TextBox_DragEnter(object sender, DragEventArgs e){
            e.Effect = DragDropEffects.Copy;
        }

        private void TextBox_DragDrop(object sender, DragEventArgs e){
            DataObject v_obj = (DataObject)e.Data;
            Session[] session_arr = (Session[]) v_obj.GetData("Fiddler.Session[]");
            Session session = session_arr[0];
            Uri url = new Uri(session.fullUrl);
            string RequestBody = Encoding.UTF8.GetString(session.RequestBody);
            string query = url.Query;
            string req_method = "get";

            // 生成Python代码
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            sb.Append("import requests\n\n");
            sb.AppendFormat("url = '{0}'\n\n", url.GetLeftPart(UriPartial.Path));
            sb.Append("headers = {\n");

            var header_arr = session.oRequest.headers.ToArray();
            foreach (HTTPHeaderItem header in header_arr) {
                if (header.Name != "Cookie") {
                    sb.AppendFormat("    '{0}': '{1}',\n", header.Name, header.Value);
                } else {
                    sb.Append("}\n\ncookies = {\n");
                    string[] cookie_pairs = header.Value.Split(';');
                    foreach (string pair in cookie_pairs) {
                        string[] parts = pair.Trim().Split('=');
                        sb.AppendFormat("    '{0}': '{1}',\n", parts[0], WebUtility.UrlDecode(parts[1]));
                    }
                    sb1.Append(", cookies=cookies");
                }
            }

            sb.Append("}\n\n");

            if (!String.IsNullOrEmpty(query)) {
                sb.Append("params = {\n");
                string[] query_pairs = query.TrimStart('?').Split('&');
                foreach (string pair in query_pairs) {
                    string[] parts = pair.Split('=');
                    sb.AppendFormat("    '{0}': '{1}',\n", parts[0], WebUtility.UrlDecode(parts[1]));
                }
                sb.Append("}\n\n");
                sb1.Append(", params=params");
            }

            if (session.RequestMethod == "POST") {
                req_method = "post";
                if (!String.IsNullOrEmpty(RequestBody)) {
                    if (RequestBody.Contains("&") && RequestBody.Contains("=")) {
                        sb.Append("data = {\n");
                        string[] body_pairs = RequestBody.Split('&');
                        foreach (string pair in body_pairs) {
                            string[] parts = pair.Split('=');
                            sb.AppendFormat("    '{0}': '{1}',\n", parts[0], WebUtility.UrlDecode(parts[1]));
                        }
                        sb.Append("}\n\n");
                    } else {
                        sb.AppendFormat("data = '{0}'\n\n", WebUtility.UrlDecode(RequestBody));
                    }
                    sb1.Append(", data=data");
                }
            }

            sb.AppendFormat("response = requests.{0}(url, headers=headers{1})\n", req_method, sb1.ToString());

            sb.Append("print(response.text)\n");

            // 显示Python代码
            codeBox.DetectUrls = false;
            codeBox.Text = sb.ToString();
        }

        public void OnLoad(){
            // 设置代码框
            codeBox = new RichTextBox();
            codeBox.AutoSize = true;
            codeBox.ReadOnly = true;
            codeBox.Multiline = true;
            codeBox.Dock = DockStyle.Fill;
            codeBox.Font = new System.Drawing.Font("Consolas", 11);
            codeBox.Text = "\n  Drag and drop the request here\n  将请求拖放到此处\n\n  https://github.com/GitCourser/fiddler2py";

            // 拖放与点击
            codeBox.AllowDrop = true;
            codeBox.DragDrop += new DragEventHandler(TextBox_DragDrop);
            codeBox.DragEnter += new DragEventHandler(TextBox_DragEnter);
            codeBox.LinkClicked += new LinkClickedEventHandler(TextBox_LinkClicked);

            // 添加选项卡
            _page = new TabPage("Python");
            _page.Controls.Add(codeBox);
            FiddlerApplication.UI.tabsViews.TabPages.Add(_page);
        }

        public void OnBeforeUnload(){}
    }
}