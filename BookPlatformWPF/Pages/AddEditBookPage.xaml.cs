using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BookPlatformWPF.Helpers;
using BookPlatformWPF.Models;

namespace BookPlatformWPF.Pages
{
    public partial class AddEditBookPage : Page
    {
        private readonly int? _bookId;

        public AddEditBookPage(int? bookId)
        {
            InitializeComponent();
            _bookId = bookId;
            LoadGenres();
            if (_bookId.HasValue)
            {
                TxtPageTitle.Text = "Редактировать книгу";
                LoadBook();
            }
        }

        private void LoadGenres()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT GenreID, Name FROM Genres ORDER BY Name", conn);
                var genres = new List<Genre>();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        genres.Add(new Genre { GenreID = (int)r["GenreID"], Name = r["Name"].ToString() });
                LstGenres.ItemsSource = genres;
            }
        }

        private void LoadBook()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT Title, Description, Content FROM Books WHERE BookID=@id", conn);
                cmd.Parameters.AddWithValue("@id", _bookId.Value);
                using (var reader = cmd.ExecuteReader())
                    if (reader.Read())
                    {
                        TxtTitle.Text = reader["Title"].ToString();
                        TxtDesc.Text = reader["Description"]?.ToString();
                        TxtContent.Text = reader["Content"]?.ToString();
                    }

                // Выбираем текущие жанры
                var selGenres = new List<int>();
                var cmd2 = new SqlCommand("SELECT GenreID FROM BookGenres WHERE BookID=@id", conn);
                cmd2.Parameters.AddWithValue("@id", _bookId.Value);
                using (var r = cmd2.ExecuteReader())
                    while (r.Read()) selGenres.Add((int)r["GenreID"]);

                foreach (Genre item in LstGenres.Items)
                    if (selGenres.Contains(item.GenreID))
                        LstGenres.SelectedItems.Add(item);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitle.Text))
            {
                LblError.Text = "Название обязательно.";
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                int bookId;
                if (_bookId.HasValue)
                {
                    var cmd = new SqlCommand(
                        "UPDATE Books SET Title=@t, Description=@d, Content=@c WHERE BookID=@id", conn);
                    cmd.Parameters.AddWithValue("@t", TxtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@d", TxtDesc.Text.Trim());
                    cmd.Parameters.AddWithValue("@c", TxtContent.Text.Trim());
                    cmd.Parameters.AddWithValue("@id", _bookId.Value);
                    cmd.ExecuteNonQuery();
                    bookId = _bookId.Value;

                    // Удаляем старые жанры
                    new SqlCommand($"DELETE FROM BookGenres WHERE BookID={bookId}", conn).ExecuteNonQuery();
                }
                else
                {
                    var cmd = new SqlCommand(
                        "INSERT INTO Books(Title,Description,Content,AuthorID,IsFrozen) OUTPUT INSERTED.BookID VALUES(@t,@d,@c,@a,0)", conn);
                    cmd.Parameters.AddWithValue("@t", TxtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@d", TxtDesc.Text.Trim());
                    cmd.Parameters.AddWithValue("@c", TxtContent.Text.Trim());
                    cmd.Parameters.AddWithValue("@a", SessionManager.UserID);
                    bookId = (int)cmd.ExecuteScalar();
                }

                // Добавляем выбранные жанры
                foreach (Genre g in LstGenres.SelectedItems)
                {
                    var ins = new SqlCommand(
                        "INSERT INTO BookGenres(BookID,GenreID) VALUES(@bid,@gid)", conn);
                    ins.Parameters.AddWithValue("@bid", bookId);
                    ins.Parameters.AddWithValue("@gid", g.GenreID);
                    ins.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Книга сохранена!", "Готово");
            MainWindow.Instance.Navigate(new AuthorPage());
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => MainWindow.Instance.Navigate(new AuthorPage());
    }
}