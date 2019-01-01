﻿/***

   Copyright (C) 2018-2019. dc-koromo. All Rights Reserved.
   
   Author: Koromo Copy Developer

***/

using Koromo_Copy;
using Koromo_Copy.Component.Hitomi;
using Koromo_Copy.Fs;
using Koromo_Copy_UX3.Domain;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Koromo_Copy_UX3.Utility
{
    /// <summary>
    /// ZipListing.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ZipListing : Window
    {
        public ZipListing()
        {
            InitializeComponent();

            foreach (var page_number in PageNumberPanel.Children)
            {
                page_number_buttons.Add(page_number as Button);
            }
            initialize_page();

            logic = new AutoCompleteBase(algorithm, SearchText, AutoComplete, AutoCompleteList);
            SearchText.GotFocus += SearchText_GotFocus;
            SearchText.LostFocus += SearchText_LostFocus;
        }

        #region UI

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var offset = AutoComplete.HorizontalOffset;
            AutoComplete.HorizontalOffset = offset + 1;
            AutoComplete.HorizontalOffset = offset;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            var offset = AutoComplete.HorizontalOffset;
            AutoComplete.HorizontalOffset = offset + 1;
            AutoComplete.HorizontalOffset = offset;
        }

        private void SearchText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText.Text))
                SearchText.Text = "검색";
        }

        private void SearchText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text == "검색")
                SearchText.Text = "";
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            PathIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            PathIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x71));
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PathIcon.Margin = new Thickness(2, 0, 0, 0);
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PathIcon.Margin = new Thickness(0, 0, 0, 0);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (SearchText.Text != "검색")
            {
                if (!string.IsNullOrEmpty(SearchText.Text.Trim()))
                {
                    elems = Search(SearchText.Text, raws);
                    SearchResult.Visibility = Visibility.Visible;
                    SearchResult.Text = $"검색결과: {elems.Count.ToString("#,#")}개";
                }
                else
                {
                    elems = raws;
                    SearchResult.Visibility = Visibility.Collapsed;
                }
                sort_data(align_column, align_row);
                max_page = elems.Count / show_elem_per_page;
                initialize_page();
            }
        }

        #endregion

        #region Search

        public static List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>> Search(string serach_text, List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>> raw)
        {
            HitomiDataQuery query = new HitomiDataQuery();
            List<string> positive_data = new List<string>();
            List<string> negative_data = new List<string>();
            List<string> request_number = new List<string>();

            serach_text.Trim().Split(' ').ToList().ForEach((a) => { if (!a.Contains(":")) positive_data.Add(a.Trim()); });
            query.Common = positive_data;
            query.Common.Add("");
            foreach (var elem in from elem in serach_text.Trim().Split(' ') where elem.Contains(":") select elem)
            {
                if (elem.StartsWith("tag:"))
                    if (query.TagInclude == null)
                        query.TagInclude = new List<string>() { elem.Substring("tag:".Length) };
                    else
                        query.TagInclude.Add(elem.Substring("tag:".Length));
                else if (elem.StartsWith("female:"))
                    if (query.TagInclude == null)
                        query.TagInclude = new List<string>() { elem };
                    else
                        query.TagInclude.Add(elem);
                else if (elem.StartsWith("male:"))
                    if (query.TagInclude == null)
                        query.TagInclude = new List<string>() { elem };
                    else
                        query.TagInclude.Add(elem);
                else if (elem.StartsWith("artist:"))
                    if (query.Artists == null)
                        query.Artists = new List<string>() { elem.Substring("artist:".Length) };
                    else
                        query.Artists.Add(elem.Substring("artist:".Length));
                else if (elem.StartsWith("series:"))
                    if (query.Series == null)
                        query.Series = new List<string>() { elem.Substring("series:".Length) };
                    else
                        query.Series.Add(elem.Substring("series:".Length));
                else if (elem.StartsWith("group:"))
                    if (query.Groups == null)
                        query.Groups = new List<string>() { elem.Substring("group:".Length) };
                    else
                        query.Groups.Add(elem.Substring("group:".Length));
                else if (elem.StartsWith("character:"))
                    if (query.Characters == null)
                        query.Characters = new List<string>() { elem.Substring("character:".Length) };
                    else
                        query.Characters.Add(elem.Substring("character:".Length));
                else if (elem.StartsWith("tagx:"))
                    if (query.TagExclude == null)
                        query.TagExclude = new List<string>() { elem.Substring("tagx:".Length) };
                    else
                        query.TagExclude.Add(elem.Substring("tagx:".Length));
                else if (elem.StartsWith("type:"))
                    if (query.Type == null)
                        query.Type = new List<string>() { elem.Substring("type:".Length) };
                    else
                        query.Type.Add(elem.Substring("type:".Length));
                else
                {
                    Koromo_Copy.Console.Console.Instance.WriteErrorLine($"Unknown rule '{elem}'.");
                    return null;
                }
            }

            var result = new List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>>();

            for (int i = 0; i < raw.Count; i++)
            {
                var v = raw[i].Item1.Value.ArticleData;
                if (query.Common.Contains(v.Id.ToString()))
                {
                    result.Add(raw[i]);
                    continue;
                }
                if (query.TagExclude != null)
                {
                    if (v.Tags != null)
                    {
                        int intersec_count = 0;
                        foreach (var tag in query.TagExclude)
                        {
                            if (v.Tags.Any(vtag => vtag.ToLower().Replace(' ', '_') == tag.ToLower()))
                            {
                                intersec_count++;
                            }

                            if (intersec_count > 0) break;
                        }
                        if (intersec_count > 0) continue;
                    }
                }
                bool[] check = new bool[query.Common.Count];
                if (query.Common.Count > 0)
                {
                    HitomiDataSearch.IntersectCountSplit(v.Title.Split(' '), query.Common, ref check);
                    HitomiDataSearch.IntersectCountSplit(v.Tags, query.Common, ref check);
                    HitomiDataSearch.IntersectCountSplit(v.Artists, query.Common, ref check);
                    HitomiDataSearch.IntersectCountSplit(v.Groups, query.Common, ref check);
                    HitomiDataSearch.IntersectCountSplit(v.Series, query.Common, ref check);
                    HitomiDataSearch.IntersectCountSplit(v.Characters, query.Common, ref check);
                }
                bool connect = false;
                if (check.Length == 0) { check = new bool[1]; check[0] = true; }
                if (check[0] && v.Artists != null && query.Artists != null) { check[0] = HitomiDataSearch.IsIntersect(v.Artists, query.Artists); connect = true; } else if (query.Artists != null) check[0] = false;
                if (check[0] && v.Tags != null && query.TagInclude != null) { check[0] = HitomiDataSearch.IsIntersect(v.Tags, query.TagInclude); connect = true; } else if (query.TagInclude != null) check[0] = false;
                if (check[0] && v.Groups != null && query.Groups != null) { check[0] = HitomiDataSearch.IsIntersect(v.Groups, query.Groups); connect = true; } else if (query.Groups != null) check[0] = false;
                if (check[0] && v.Series != null && query.Series != null) { check[0] = HitomiDataSearch.IsIntersect(v.Series, query.Series); connect = true; } else if (query.Series != null) check[0] = false;
                if (check[0] && v.Characters != null && query.Characters != null) { check[0] = HitomiDataSearch.IsIntersect(v.Characters, query.Characters); connect = true; } else if (query.Characters != null) check[0] = false;
                if (check[0] && v.Types != null && query.Type != null) { check[0] = query.Type.Any(x => x == v.Types); connect = true; } else if (query.Type != null) check[0] = false;
                if (check.All((x => x)) && ((query.Common.Count == 0 && connect) || query.Common.Count > 0))
                    result.Add(raw[i]);
            }

            result.Sort((a, b) => Convert.ToInt32(b.Item1.Value.ArticleData.Id) - Convert.ToInt32(a.Item1.Value.ArticleData.Id));
            return result;
        }

        #endregion

        #region UI / IO

        List<KeyValuePair<string, ZipListingArticleModel>> article_list;
        ZipListingModel model;

        int show_elem_per_page = 20;

        private async void ProcessPath(string path)
        {
            FileIndexor fi = new FileIndexor();
            await fi.ListingDirectoryAsync(path);

            string root_directory = fi.RootDirectory;

            Dictionary<string, ZipListingArticleModel> article_dic = new Dictionary<string, ZipListingArticleModel>();
            foreach (var x in fi.Directories)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(
                delegate
                {
                    ProgressText.Text = x.Item1;
                }));
                foreach (var file in x.Item3)
                {
                    try
                    {
                        if (!file.FullName.EndsWith(".zip")) continue;
                        var zipFile = ZipFile.Open(file.FullName, ZipArchiveMode.Read);
                        if (zipFile.GetEntry("Info.json") == null) continue;
                        using (var reader = new StreamReader(zipFile.GetEntry("Info.json").Open()))
                        {
                            var json_model = JsonConvert.DeserializeObject<HitomiJsonModel>(reader.ReadToEnd());
                            article_dic.Add(file.FullName.Substring(root_directory.Length), new ZipListingArticleModel { ArticleData = json_model, Size = file.Length, CreatedDate = file.CreationTime.ToString() });
                        }
                    }
                    catch (Exception e)
                    {
                        Monitor.Instance.Push($"[Zip Listing] {e.Message}");
                    }
                }
            }

            model = new ZipListingModel();
            model.RootDirectory = root_directory;
            model.Tag = path;
            model.ArticleList = article_dic.ToArray();
            ZipListingModelManager.SaveModel($"ziplist-result-{DateTime.Now.Ticks}.json", model);

            algorithm.Build(model);
            article_list = article_dic.ToList();
            elems.Clear();
            article_list.ForEach(x => elems.Add(Tuple.Create(x, new Lazy<ZipListingElements>(() =>
            {
                return new ZipListingElements(root_directory + x.Key);
            }))));
            raws = elems;
            sort_data(align_column, align_row);

            await Application.Current.Dispatcher.BeginInvoke(new Action(
            delegate
            {
                CollectStatusPanel.Visibility = Visibility.Collapsed;
                ArticleCount.Text = $"작품 {article_dic.Count.ToString("#,#")}개";
                PageCount.Text = $"이미지 {article_list.Select(x => x.Value.ArticleData.Pages).Sum().ToString("#,#")}장";
                max_page = article_dic.Count / show_elem_per_page;
                initialize_page();
            }));
        }

        int align_row = 0;
        int align_column = 0;
        private async void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;

            if (item.Tag.ToString() == "New")
            {
                var cofd = new CommonOpenFileDialog();
                cofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                cofd.IsFolderPicker = true;
                if (cofd.ShowDialog(this) == CommonFileDialogResult.Ok)
                {
                    CollectStatusPanel.Visibility = Visibility.Visible;
                    await Task.Run(() => ProcessPath(cofd.FileName));
                }
            }
            else if (item.Tag.ToString() == "Open")
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                ofd.Filter = "데이터 파일 (*.json)|*.json";
                if (ofd.ShowDialog() == true)
                {
                    // 열기
                    try
                    {
                        model = ZipListingModelManager.OpenModel(ofd.FileName);
                    }
                    catch
                    {
                        MessageBox.Show("옳바른 파일이 아닙니다!", "Zip Listing", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (!Directory.Exists(model.RootDirectory))
                    {
                        MessageBox.Show($"루트 디렉토리 {model.RootDirectory}를 찾을 수 없습니다! 루트 디렉토리를 수정해주세요!", "Zip Listing", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    algorithm.Build(model);
                    article_list = model.ArticleList.ToList();

                    // 초기화
                    elems.Clear();
                    article_list.ForEach(x => elems.Add(Tuple.Create(x, new Lazy<ZipListingElements>(() =>
                    {
                        return new ZipListingElements(model.RootDirectory + x.Key);
                    }))));
                    raws = elems;
                    sort_data(align_column, align_row);
                    ArticleCount.Text = $"작품 {article_list.Count.ToString("#,#")}개";
                    PageCount.Text = $"이미지 {article_list.Select(x=>x.Value.ArticleData.Pages).Sum().ToString("#,#")}장";
                    max_page = article_list.Count / show_elem_per_page;
                    initialize_page();
                }
            }
            else if (item.Tag.ToString() == "Align")
            {
                var dialog = new ZipListingSorting(align_column, align_row);
                if ((bool)(await DialogHost.Show(dialog, "RootDialog")))
                {
                    int column = dialog.AlignColumnIndex;
                    int row = dialog.AlignRowIndex;

                    if (column == align_column && row == align_row) return;
                    sort_data(column, row);
                    align_column = column;
                    align_row = row;
                    initialize_page();
                }
            }
            else if (item.Tag.ToString() == "Statistics")
            {

            }
        }

        private void sort_data(int column, int row)
        {
            if (column == 0)
            {
                elems.Sort((x, y) => SortAlgorithm.ComparePath(x.Item1.Key, y.Item1.Key));
            }
            else if (column == 1)
            {
                elems.Sort((x, y) => DateTime.Parse(x.Item1.Value.CreatedDate).CompareTo(DateTime.Parse(y.Item1.Value.CreatedDate)));
            }
            else if (column == 2)
            {
                elems.Sort((x, y) => x.Item1.Value.Size.CompareTo(y.Item1.Value.Size));
            }
            else if (column == 3)
            {
                elems.Sort((x, y) => x.Item1.Value.ArticleData.Title.CompareTo(y.Item1.Value.ArticleData.Title));
            }
            else if (column == 4)
            {
                elems.Sort((x, y) => x.Item1.Value.ArticleData.Id.CompareTo(y.Item1.Value.ArticleData.Id));
            }
            else if (column == 5)
            {
                elems.Sort((x, y) => x.Item1.Value.ArticleData.Pages.CompareTo(y.Item1.Value.ArticleData.Pages));
            }

            if (row == 1) elems.Reverse();
        }

        List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>> raws = new List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>>();
        List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>> elems = new List<Tuple<KeyValuePair<string, ZipListingArticleModel>, Lazy<ZipListingElements>>>();
        private void show_page_impl(int page)
        {
            SeriesPanel.Children.Clear();

            for (int i = page * show_elem_per_page; i < (page + 1) * show_elem_per_page && i < elems.Count; i++)
            {
                SeriesPanel.Children.Add(elems[i].Item2.Value);
            }
        }

        #endregion

        #region Pager

        int max_page = 0; // 1 ~ 250
        int current_page_segment = 0;

        List<Button> page_number_buttons = new List<Button>();

        private void initialize_page()
        {
            current_page_segment = 0;
            page_number_buttons.ForEach(x => x.Visibility = Visibility.Visible);
            set_page_segment(0);
            show_page(0);
        }

        private void show_page(int i)
        {
            page_number_buttons.ForEach(x => {
                x.Background = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30));
                x.Foreground = new SolidColorBrush(Color.FromRgb(0x71, 0x71, 0x71));
            });
            page_number_buttons[i % 10].Background = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
            page_number_buttons[i % 10].Foreground = new SolidColorBrush(Color.FromRgb(0x17, 0x17, 0x17));

            show_page_impl(i);
        }

        private void set_page_segment(int seg)
        {
            for (int i = 0, j = current_page_segment * 10; i < 10; i++, j++)
            {
                page_number_buttons[i].Content = (j + 1).ToString();

                if (j <= max_page)
                    page_number_buttons[i].Visibility = Visibility.Visible;
                else
                    page_number_buttons[i].Visibility = Visibility.Collapsed;
            }
        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            show_page(Convert.ToInt32((string)(sender as Button).Content) - 1);
        }

        private void PageFunction_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Tag.ToString())
            {
                case "LeftLeft":
                    if (current_page_segment == 0) break;

                    current_page_segment = 0;
                    set_page_segment(0);
                    show_page(0);
                    break;

                case "Left":
                    if (current_page_segment == 0) break;

                    current_page_segment--;
                    set_page_segment(current_page_segment);
                    show_page(current_page_segment * 10);
                    break;

                case "Right":
                    if (max_page < 10) break;
                    if (current_page_segment == max_page / 10) break;

                    current_page_segment++;
                    set_page_segment(current_page_segment);
                    show_page(current_page_segment * 10);
                    break;

                case "RightRight":
                    if (max_page < 10) break;
                    if (current_page_segment == max_page / 10) break;

                    current_page_segment = max_page / 10;
                    set_page_segment(current_page_segment);
                    show_page(max_page);
                    break;
            }
        }

        #endregion

        #region Search Helper
        ZipListingAutoComplete algorithm = new ZipListingAutoComplete();
        AutoCompleteBase logic;

        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (SearchText.Text != "검색")
                {
                    if (e.Key == Key.Return && !logic.skip_enter)
                    {
                        ButtonAutomationPeer peer = new ButtonAutomationPeer(SearchButton);
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv.Invoke();
                    }
                    logic.skip_enter = false;
                }
            }
        }

        private void SearchText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;
            logic.SearchText_PreviewKeyDown(sender, e);
        }

        private void SearchText_KeyUp(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;
            logic.SearchText_KeyUp(sender, e);
        }

        private void AutoCompleteList_KeyUp(object sender, KeyEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;
            logic.AutoCompleteList_KeyUp(sender, e);
        }

        private void AutoCompleteList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AutoCompleteList.Width = SearchText.RenderSize.Width;
            logic.AutoCompleteList_MouseDoubleClick(sender, e);
        }
        #endregion
    }
}
