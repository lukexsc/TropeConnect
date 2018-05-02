using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;

namespace TropeConnect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, int> tropes; // dictionary of all tropes and their frequency
        List<string> currentPageTropes; // list of tropes on the current page
        string[] links; // array of all pages to check through

        public MainWindow()
        {
            InitializeComponent();

            tropes = new Dictionary<string, int>();
            currentPageTropes = new List<string>();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Clear Tropes
            tropes.Clear();

            // Get list of Addresses
            string textInput = inputTextBox.Text;
            char[] delimiterChars = { ' ', ',', '\t', '\n' };
            links = textInput.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

            // Show Loading
            inputTextBox.IsEnabled = false;
            analyzeButton.IsEnabled = false;

            Task.Delay(100).ContinueWith(_ =>
            {
                // Go to each Link
                foreach (string url in links)
                {
                    // Set Loading Text
                    this.Dispatcher.Invoke(() =>
                    {
                        loadingText.Text = "Reading " + url;
                    });

                    try
                    {
                        // Add Links to current page list
                        currentPageTropes.Clear();
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        StreamReader sr = new StreamReader(response.GetResponseStream());
                        HtmlDocument doc = new HtmlDocument();
                        doc.Load(sr);
                        var aTags = doc.DocumentNode.SelectNodes("//div[contains(@class, 'page-content')]//a[contains(@class, 'twikilink')]");
                        if (aTags != null)
                        {
                            foreach (var aTag in aTags)
                            {
                                string link = aTag.Attributes["href"].Value;
                                if (!currentPageTropes.Contains(link))
                                    currentPageTropes.Add(link);
                            }
                        }
                        sr.Close();

                        // Add to dictionary
                        foreach (string trope in currentPageTropes)
                        {
                            if (!tropes.ContainsKey(trope)) // add new trope
                                tropes.Add(trope, 1);
                            else // trope exists - increment
                                tropes[trope] += 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Had Error, do nothing
                    }
                }

                // Hide Loading
                this.Dispatcher.Invoke(() =>
                {
                    // Sort Tropes by number in common
                    var sortedDict = from entry in tropes orderby entry.Value descending select entry;

                    string outputText = "";
                    int currentCount = -1;
                    foreach (KeyValuePair<string, int> trope in sortedDict)
                    {
                        if (trope.Value <= 1) break;
                        if (trope.Value != currentCount)
                        {
                            if (currentCount != -1) outputText += "\n\n";
                            currentCount = trope.Value;
                            outputText += "<" + currentCount + ">";
                        }
                        outputText += "\n" + trope.Key;
                    }

                    inputTextBox.IsEnabled = true;
                    loadingText.Text = "";
                    analyzeButton.IsEnabled = true;

                    OutputWindow outputWindow = new OutputWindow(outputText);
                    outputWindow.Show();
                });
            });
        }
    }
}
