using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsAppListBox
{
    public partial class Form1 : Form
    {
        private readonly ImageList smallImageList = new ImageList();

        public Form1()
        {
            InitializeComponent();

            // We will use this as a cache that we build over time
            listView1.SmallImageList = smallImageList;
        }

        private void Form1_Shown( object sender, EventArgs e )
        {
            PostUpdate( Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments ) );
        }

        private void PostUpdate( string path )
        {
            if ( listView1.InvokeRequired )
            {
                MessageBox.Show( "DEV: Call this on event thread" );
                return;
            }

            listView1.Items.Clear();

            Task.Run( () =>
            {
                Update( path );
            } );
        }

        private void Update( string path )
        {
            try
            {
                var dirs = new List<DirectoryInfo>();
                foreach ( var dir in Directory.GetDirectories( path ) )
                {
                    dirs.Add( new DirectoryInfo( dir ) );
                }

                var files = new List<FileInfo>();
                foreach ( var file in Directory.GetFiles( path ) )
                {
                    files.Add( new FileInfo( file ) );
                }

                Display( path, dirs, files );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( $"Failed: {ex.Message}" );
            }
        }

        private void Display( string path, List<DirectoryInfo> dirs, List<FileInfo> files )
        {
            if ( listView1.InvokeRequired )
            {
                listView1.Invoke( (MethodInvoker) delegate 
                {
                    Display( path, dirs, files );
                } );
                return;
            }

            try
            {
                textBox1.Text = path;
                listView1.Tag = path;

                listView1.BeginUpdate();

                foreach ( var dir in dirs )
                {
                    var lvi = new ListViewItem( dir.Name );

                    if ( !smallImageList.Images.ContainsKey( dir.FullName ) )
                    {
                        smallImageList.Images.Add( dir.FullName, Icons.GetSmallIcon( dir.FullName ) );
                    }
                    lvi.ImageKey = dir.FullName;
                    lvi.Tag = dir;

                    listView1.Items.Add( lvi );
                }

                foreach ( var file in files )
                {
                    var lvi = new ListViewItem( file.Name );

                    if ( !smallImageList.Images.ContainsKey( file.FullName ) )
                    {
                        smallImageList.Images.Add( file.FullName, Icons.GetSmallIcon( file.FullName ) );
                    }
                    lvi.ImageKey = file.FullName;
                    lvi.Tag = file;

                    listView1.Items.Add( lvi );
                }
            }
            finally 
            { 
                listView1.EndUpdate();
            }
        }

        private void listView1_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return || e.KeyCode == Keys.Right )
            {
                if ( listView1.SelectedItems.Count > 0 )
                {
                    var lvi = listView1.SelectedItems[ 0 ];
                    if ( lvi.Tag is DirectoryInfo )
                    {
                        PostUpdate( ( lvi.Tag as DirectoryInfo ).FullName );
                    }
                }
            }
            else if ( e.KeyCode == Keys.Back || e.KeyCode == Keys.Left )
            {
                var path = listView1.Tag as string;
                PostUpdate( Directory.GetParent( path ).FullName );
            }
        }

        private void textBox1_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return )
            {
                if ( textBox1.Text.Length > 0 )
                {
                    PostUpdate( textBox1.Text );
                }
            }
        }
    }
}
