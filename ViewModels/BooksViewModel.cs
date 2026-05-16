using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using LibraryManagementFE.Models;
using LibraryManagementFE.Views;

namespace LibraryManagementFE.ViewModels
{
    /// <summary>MVVM layer for Quản lý Sách (Figma: search, table, pagination, stats).</summary>
    public class BooksViewModel : INotifyPropertyChanged
    {
        private const int PageSize = 5;

        public ObservableCollection<BookRecord> Books { get; } = new()
        {
            new BookRecord
            {
                Stt = 1,
                Title = "Clean Code",
                Author = "Robert C. Martin",
                CategoryLine1 = "CNTT",
                CategoryLine2 = "TECH",
                CategoryPillBg = "#EFF6FF",
                CategoryPillFg = "#1978E5",
                Year = 2008,
                Availability = BookAvailability.SanCo,
            },
            new BookRecord
            {
                Stt = 2,
                Title = "Introduction to Algorithms",
                Author = "Thomas H. Cormen",
                CategoryLine1 = "CNTT",
                CategoryLine2 = "TECH",
                CategoryPillBg = "#EFF6FF",
                CategoryPillFg = "#1978E5",
                Year = 2009,
                Availability = BookAvailability.DangMuon,
            },
            new BookRecord
            {
                Stt = 3,
                Title = "Mắt Biếc",
                Author = "Nguyễn Nhật Ánh",
                CategoryLine1 = "Văn",
                CategoryLine2 = "học",
                CategoryPillBg = "#FAF5FF",
                CategoryPillFg = "#9333EA",
                Year = 1990,
                Availability = BookAvailability.SanCo,
            },
        };

        public ObservableCollection<BookRecord> PagedBooks { get; } = new();
        public ObservableCollection<PageNumberItem> PageNumbers { get; } = new();

        public string TotalBooksDisplay => "1.240";
        public string NewThisMonthDisplay => "42";
        public string BorrowedDisplay => "86";

        public int TotalPages => Math.Max(1, (int)Math.Ceiling(Books.Count / (double)PageSize));

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage == value)
                {
                    return;
                }

                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }

        public string PaginationInfo
        {
            get
            {
                if (Books.Count == 0)
                {
                    return "Hiển thị 0 của 0 đầu sách";
                }

                var start = ((CurrentPage - 1) * PageSize) + 1;
                var end = Math.Min(CurrentPage * PageSize, Books.Count);
                return $"Hiển thị {start} - {end} của {Books.Count} đầu sách";
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); }
        }

        public ICommand FilterCommand { get; }
        public ICommand AddBookCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand DeleteBookCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand GoToPageCommand { get; }

        public BooksViewModel()
        {
            FilterCommand = new RelayCommand(_ =>
                MessageBox.Show("Bộ lọc nâng cao đang phát triển.", "Thông báo", MessageBoxButton.OK));

            AddBookCommand = new RelayCommand(OpenAddBookDialog);

            EditBookCommand = new RelayCommand(p =>
            {
                if (p is BookRecord book)
                {
                    OpenEditBookDialog(book);
                }
            });

            DeleteBookCommand = new RelayCommand(p =>
            {
                if (p is BookRecord book)
                {
                    OpenDeleteBookDialog(book);
                }
            });

            PrevPageCommand = new RelayCommand(
                _ => GoToPage(CurrentPage - 1),
                _ => CurrentPage > 1);

            NextPageCommand = new RelayCommand(
                _ => GoToPage(CurrentPage + 1),
                _ => CurrentPage < TotalPages);

            GoToPageCommand = new RelayCommand(p =>
            {
                if (p is int page)
                {
                    GoToPage(page);
                    return;
                }

                if (int.TryParse(p?.ToString(), out page))
                {
                    GoToPage(page);
                }
            });

            RefreshPagedBooks();
        }

        private void OpenAddBookDialog(object? _)
        {
            var dialog = new AddBookWindow
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var book in dialog.Books)
                {
                    book.Stt = Books.Count + 1;
                    Books.Add(book);
                }

                GoToPage(TotalPages);
            }
        }

        private void OpenEditBookDialog(BookRecord book)
        {
            var dialog = new AddBookWindow(book)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            if (dialog.ShowDialog() == true && dialog.Books.FirstOrDefault() is BookRecord editedBook)
            {
                var index = Books.IndexOf(book);
                if (index < 0)
                {
                    return;
                }

                editedBook.Stt = book.Stt;
                Books[index] = editedBook;
                RefreshPagedBooks();
            }
        }

        private void OpenDeleteBookDialog(BookRecord book)
        {
            var dialog = new ConfirmDeleteWindow("sách", book.Title)
            {
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            if (dialog.ShowDialog() == true)
            {
                Books.Remove(book);
                ReorderBooks();
                GoToPage(Math.Min(CurrentPage, TotalPages));
            }
        }

        private void ReorderBooks()
        {
            for (var index = 0; index < Books.Count; index++)
            {
                Books[index].Stt = index + 1;
            }
        }

        private void GoToPage(int page)
        {
            CurrentPage = Math.Clamp(page, 1, TotalPages);
            RefreshPagedBooks();
        }

        private void RefreshPagedBooks()
        {
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            PagedBooks.Clear();
            foreach (var book in Books.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            {
                PagedBooks.Add(book);
            }

            RefreshPageNumbers();
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PaginationInfo));
            CommandManager.InvalidateRequerySuggested();
        }

        private void RefreshPageNumbers()
        {
            PageNumbers.Clear();
            for (var page = 1; page <= TotalPages; page++)
            {
                PageNumbers.Add(new PageNumberItem(page, page == CurrentPage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PageNumberItem
    {
        public PageNumberItem(int number, bool isCurrent)
        {
            Number = number;
            IsCurrent = isCurrent;
        }

        public int Number { get; }
        public bool IsCurrent { get; }
    }
}
