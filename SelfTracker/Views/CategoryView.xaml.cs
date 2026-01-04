using SelfTracker.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static SelfTracker.Repository.DataRepository;

namespace SelfTracker.Views
{
    public class CategoryItem
    {
        public string Name { get; set; }
    }

    public partial class CategoryView : System.Windows.Controls.UserControl
    {
        private readonly DataRepository _repo;
        private ProcessCategoryInfo _selectedProcess;
        private List<ProcessCategoryInfo> _allProcesses;
        private string _editingCategory; // 用于标签编辑

        public CategoryView()
        {
            InitializeComponent();
            var dbService = new SQLiteDataService();
            _repo = new DataRepository(dbService.ConnectionString);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAll();
        }

        /// <summary>
        /// 全局刷新：加载所有数据并更新界面
        /// </summary>
        private void RefreshAll()
        {
            // 1. 加载所有进程数据
            _allProcesses = _repo.GetFullProcessCategoryMap();

            // 2. 应用当前筛选条件
            ApplyFilter();

            // 3. 刷新标签列表
            RefreshCategories();

            // 4. 更新统计信息
            UpdateStatistics();
        }

        /// <summary>
        /// 刷新标签列表（标签管理区和分类选择区）
        /// </summary>
        private void RefreshCategories()
        {
            List<string> categories = _repo.GetAllCategoryDefinitions();

            // 更新标签管理区
            CategoryItemsControl.ItemsSource = null;
            CategoryItemsControl.ItemsSource = categories;

            // 更新分类选择区
            CategorySelectionControl.ItemsSource = null;
            CategorySelectionControl.ItemsSource = categories;

            // 更新标签计数
            TxtCategoryCount.Text = $"({categories.Count} 个标签)";
        }

        /// <summary>
        /// 应用筛选条件
        /// </summary>
        private void ApplyFilter()
        {
            if (_allProcesses == null) return;

            List<ProcessCategoryInfo> filtered;

            if (RbCategorized.IsChecked == true)
            {
                // 已分类（不是"未分类"）
                filtered = _allProcesses.Where(p => p.Category != "未分类").ToList();
            }
            else if (RbUncategorized.IsChecked == true)
            {
                // 未分类
                filtered = _allProcesses.Where(p => p.Category == "未分类").ToList();
            }
            else
            {
                // 全部
                filtered = _allProcesses;
            }

            ProcessListBox.ItemsSource = filtered;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            if (_allProcesses == null) return;

            int total = _allProcesses.Count;
            int categorized = _allProcesses.Count(p => p.Category != "未分类");
            int uncategorized = total - categorized;

            TxtTotalCount.Text = $"总计: {total}";
            TxtCategorizedCount.Text = $"已分类: {categorized}";
            TxtUncategorizedCount.Text = $"未分类: {uncategorized}";
        }

        // ==================== 事件处理 ====================

        /// <summary>
        /// 筛选条件变化
        /// </summary>
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            ApplyFilter();
        }

        /// <summary>
        /// 进程列表选择变化
        /// </summary>
        private void ProcessListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedProcess = ProcessListBox.SelectedItem as ProcessCategoryInfo;

            if (_selectedProcess != null)
            {
                TxtSelectedProcess.Text = _selectedProcess.ProcessName;
                TxtCurrentCategory.Text = _selectedProcess.Category;
            }
            else
            {
                TxtSelectedProcess.Text = "未选择";
                TxtCurrentCategory.Text = "-";
            }
        }

        // ==================== 标签管理 ====================

        /// <summary>
        /// 添加新标签
        /// </summary>
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            AddNewCategory();
        }

        private void TxtNewCategory_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewCategory();
            }
        }

        private void AddNewCategory()
        {
            string newCategory = TxtNewCategory.Text.Trim();

            if (string.IsNullOrWhiteSpace(newCategory))
            {
                System.Windows.MessageBox.Show("请输入标签名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 检查是否已存在
            var existingCategories = _repo.GetAllCategoryDefinitions();
            if (existingCategories.Contains(newCategory))
            {
                System.Windows.MessageBox.Show($"标签 [{newCategory}] 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 添加到数据库
            _repo.AddCategoryDefinition(newCategory);

            // 清空输入框
            TxtNewCategory.Clear();

            // 刷新标签列表
            RefreshCategories();

            System.Windows.MessageBox.Show($"标签 [{newCategory}] 添加成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 编辑标签
        /// </summary>
        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            string categoryName = btn.Tag.ToString();
            _editingCategory = categoryName;

            // 查找对应的 Border 容器
            Border border = FindVisualParent<Border>(btn);
            if (border == null) return;

            // 查找 TextBlock 和 TextBox
            TextBlock textBlock = FindVisualChild<TextBlock>(border, "CategoryTextBlock");
            System.Windows.Controls.TextBox textBox = FindVisualChild<System.Windows.Controls.TextBox>(border, "CategoryEditBox");

            if (textBlock != null && textBox != null)
            {
                // 切换显示
                textBlock.Visibility = Visibility.Collapsed;
                textBox.Visibility = Visibility.Visible;
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        /// <summary>
        /// 标签编辑框键盘事件
        /// </summary>
        private void CategoryEditBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            if (e.Key == Key.Enter)
            {
                SaveCategoryEdit(textBox);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCategoryEdit(textBox);
            }
        }

        /// <summary>
        /// 标签编辑框失去焦点
        /// </summary>
        private void CategoryEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            SaveCategoryEdit(textBox);
        }

        /// <summary>
        /// 保存标签编辑
        /// </summary>
        private void SaveCategoryEdit(System.Windows.Controls.TextBox textBox)
        {
            string newName = textBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                System.Windows.MessageBox.Show("标签名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                CancelCategoryEdit(textBox);
                return;
            }

            if (newName == _editingCategory)
            {
                // 没有修改
                CancelCategoryEdit(textBox);
                return;
            }

            // 检查新名称是否已存在
            var existingCategories = _repo.GetAllCategoryDefinitions();
            if (existingCategories.Contains(newName))
            {
                System.Windows.MessageBox.Show($"标签 [{newName}] 已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                CancelCategoryEdit(textBox);
                return;
            }

            // TODO: 调用仓储层的重命名方法
            // _repo.RenameCategoryDefinition(_editingCategory, newName);

            System.Windows.MessageBox.Show("标签重命名功能待实现（仓储层方法缺失）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            CancelCategoryEdit(textBox);
        }

        /// <summary>
        /// 取消标签编辑
        /// </summary>
        private void CancelCategoryEdit(System.Windows.Controls.TextBox textBox)
        {
            Border border = FindVisualParent<Border>(textBox);
            if (border == null) return;

            TextBlock textBlock = FindVisualChild<TextBlock>(border, "CategoryTextBlock");

            if (textBlock != null)
            {
                textBox.Visibility = Visibility.Collapsed;
                textBlock.Visibility = Visibility.Visible;
            }

            _editingCategory = null;
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        private void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            string categoryName = btn.Tag.ToString();

            var result = System.Windows.MessageBox.Show(
                $"确定要删除标签 [{categoryName}] 吗？\n\n所有使用此标签的应用将变为 '未分类'。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _repo.DeleteCategoryDefinition(categoryName);
                RefreshAll();
                System.Windows.MessageBox.Show($"标签 [{categoryName}] 已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ==================== 分类操作 ====================

        /// <summary>
        /// 应用分类到选中的进程
        /// </summary>
        private void ApplyCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProcess == null)
            {
                System.Windows.MessageBox.Show("请先从左侧列表选择一个应用", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            string category = btn.Tag.ToString();

            // 1. 立即备份需要使用的 ID 或名称，不要依赖容易变动的 _selectedProcess 对象
            string targetProcessName = _selectedProcess.ProcessName;

            if (_selectedProcess.Category == category)
            {
                System.Windows.MessageBox.Show($"该应用已经是 [{category}] 分类", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 应用分类
            _repo.ApplyProcessCategory(targetProcessName, category);

            // 2. 刷新数据（这会导致 _selectedProcess 变为 null）
            RefreshAll();

            // 3. 使用备份的名称在刷新后的集合中查找
            var updatedProcess = _allProcesses.FirstOrDefault(p => p.ProcessName == targetProcessName);

            if (updatedProcess != null)
            {
                // 4. 重新选中。注意：设置 SelectedItem 又会触发 SelectionChanged，
                // 从而重新正确给 _selectedProcess 赋值
                ProcessListBox.SelectedItem = updatedProcess;
                ProcessListBox.ScrollIntoView(updatedProcess);
            }

            System.Windows.MessageBox.Show($"已将 [{targetProcessName}] 设置为 [{category}]", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 查找可视化树的父元素
        /// </summary>
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindVisualParent<T>(parentObject);
            }
        }

        /// <summary>
        /// 查找可视化树的子元素（按名称）
        /// </summary>
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }

                T result = FindVisualChild<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
