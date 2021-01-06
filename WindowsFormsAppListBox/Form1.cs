using Microsoft.Win32;
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

namespace Highbrow
{
    /// <summary>
    /// Intentions:
    /// <list type="bullet">
    /// <item>Rename</item>
    /// <item>Delete</item>
    /// <item>Search</item>
    /// <item>Filter</item>
    /// <item>Open Terminal here</item>
    /// <item>Open Pane</item>
    /// <item>Close Pane</item>
    /// <item>Dock/Undock Pane?</item>
    /// </list>
    /// 
    /// Completed:
    /// 
    /// </summary>
    public partial class Form1 : Form
    {
        private readonly ImageList smallImageList = new ImageList();

        private readonly Dictionary<string, string> FileTypeCache = new Dictionary<string, string>();

        private string initialPath = Environment.GetFolderPath( Environment.SpecialFolder.MyDocuments );

        public Form1( string[] args )
        {
            InitializeComponent();

            // We will use this as a cache that we build over time
            listView1.SmallImageList = smallImageList;
            listView1.SmallImageList.ImageSize = new Size( 20, 20 );
            if ( args.Length > 0 )
            {
                // Allow a path on the command line, but validate it before we use it
                if ( Directory.Exists( args[ 0 ] ) )
                {
                    initialPath = args[ 0 ];
                }
                else
                {
                    Invoke( (MethodInvoker) delegate 
                    {
                        MessageBox.Show( $"{args[0]} is not a valid path" );
                    } );
                }
            }
        }

        private void Form1_Shown( object sender, EventArgs e )
        {
            PostUpdate( initialPath );
        }

        private void PostUpdate( string path )
        {
            if ( listView1.InvokeRequired )
            {
                MessageBox.Show( "DEV: Call this on event thread" );
                return;
            }

            Task.Run( () =>
            {
                Update( path );
            } );
        }

        private void Update( string path )
        {
            var fullPath = path;
            try
            {
                // Add a little convenience thing of ~ for home directory - make it an option?
                if ( fullPath.StartsWith( "~" ) )
                {
                    // Prune following slashes - but only in the case of ~
                    var remnant = fullPath.Substring( 1 );
                    while ( remnant.StartsWith( "/" ) || remnant.StartsWith( @"\" ) )
                    {
                        remnant = remnant.Substring( 1 );
                    }
                    fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), remnant );
                }

                // Do we really want to do this? I guess so - make it an option?
                fullPath = fullPath.Replace( "/", @"\" );

                var dirs = new List<DirectoryInfo>();
                foreach ( var dir in Directory.GetDirectories( fullPath ) )
                {
                    dirs.Add( new DirectoryInfo( dir ) );
                }

                var files = new List<FileInfo>();
                foreach ( var file in Directory.GetFiles( fullPath ) )
                {
                    files.Add( new FileInfo( file ) );
                }

                Display( path, dirs, files );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( $"Failed: {ex.Message}" );

                // Make sure we go back to last good value
                Update( listView1.Tag as string );
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
                UseWaitCursor = true;

                var from = listView1.Tag as string;
                var focused = false;

                listView1.BeginUpdate();
                listView1.Items.Clear();

                textBox1.Text = path;
                listView1.Tag = path;

                foreach ( var dir in dirs )
                {
                    var lvi = new ListViewItem( dir.Name );

                    if ( !smallImageList.Images.ContainsKey( dir.FullName ) )
                    {
//                        smallImageList.Images.Add( dir.FullName, Icons.GetSmallIcon( dir.FullName ) );
                        smallImageList.Images.Add( dir.FullName, Icons.GetPaddedIcon( Icons.GetSmallIcon( dir.FullName ) ) );
                    }
                    lvi.ImageKey = dir.FullName;
                    lvi.Tag = dir;

                    lvi.SubItems.Add( GetDateString( dir.LastWriteTime ) );
                    lvi.SubItems.Add( "" ); // Size
                    lvi.SubItems.Add( "File folder" );

                    listView1.Items.Add( lvi );

                    if ( dir.FullName == from )
                    {
                        focused = true;
                        lvi.Focused = true;
                        lvi.Selected = true; 
                    }
                }

                foreach ( var file in files )
                {
                    var lvi = new ListViewItem( file.Name );

                    if ( !smallImageList.Images.ContainsKey( file.FullName ) )
                    {
                        //smallImageList.Images.Add( file.FullName, Icons.GetSmallIcon( file.FullName ) );
                        smallImageList.Images.Add( file.FullName, Icons.GetPaddedIcon( Icons.GetSmallIcon( file.FullName ) ) );
                    }
                    lvi.ImageKey = file.FullName;
                    lvi.Tag = file;

                    lvi.SubItems.Add( GetDateString( file.LastWriteTime ) );
                    lvi.SubItems.Add( GetSizeString( file.Length ) ); // Size
                    lvi.SubItems.Add( GetTypeString( file.Extension ) );
                    
                    listView1.Items.Add( lvi );
                }

                if ( !focused && listView1.Items.Count > 0 )
                {
                    listView1.Items[ 0 ].Focused = true;
                }
            }
            finally 
            { 
                listView1.EndUpdate();

                UseWaitCursor = false;
            }
        }

        private void listView1_KeyDown( object sender, KeyEventArgs e )
        {
            if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return || e.KeyCode == Keys.Right )
            {
                if ( listView1.SelectedItems.Count > 0 )
                {
                    var lvi0 = listView1.SelectedItems[ 0 ];
                    if ( lvi0.Tag is DirectoryInfo )
                    {
                        PostUpdate( ( lvi0.Tag as DirectoryInfo ).FullName );
                    }
                    else if ( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return )
                    {
                        foreach ( ListViewItem lvi in listView1.SelectedItems )
                        {
                            if ( lvi.Tag is FileInfo )
                            {
                                System.Diagnostics.Process.Start( ( lvi.Tag as FileInfo ).FullName );
                            }
                        }
                    }
                }
            }
            else if ( e.KeyCode == Keys.Back || e.KeyCode == Keys.Left )
            {
                var path = listView1.Tag as string;
                var parent = Directory.GetParent( path );
                if ( parent != null )
                {
                    PostUpdate( parent.FullName );
                }
                else
                {
                    // Get roots?
                }
            }
            else if ( e.KeyCode == Keys.D && e.Alt )
            {
                textBox1.Focus();
                textBox1.SelectAll();
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
            else if ( e.KeyCode == Keys.Down )
            {
                listView1.Focus();
                listView1.FocusedItem.Selected = true;
            }
            else if ( e.KeyCode == Keys.D && e.Alt )
            {
                textBox1.SelectAll();
            }
        }

        private string GetDateString( DateTime datetime )
        {
            return $"{datetime.ToShortDateString()} {datetime.ToShortTimeString()}";
        }

        private string GetSizeString( long size )
        {
            var kb = (double) size / 1024;
            return $"{Math.Ceiling(kb):n0} KB";
        }

        private string GetTypeString( string extension )
        {
            if ( string.IsNullOrEmpty( extension ) )
            {
                return "File";
            }

            if ( !FileTypeCache.ContainsKey( extension ) )
            {
                // Default, but then try and improve
                FileTypeCache[ extension ] = extension;

                var desc = FileTypes.GetFileTypeDescription( extension );
                if ( !string.IsNullOrEmpty( desc ) )
                {
                    FileTypeCache[ extension ] = desc;
                }
            }

            return FileTypeCache[ extension ];
        }
    }
}
