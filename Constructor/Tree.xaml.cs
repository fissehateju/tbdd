using TBDD.Dataplane;
using TBDD.Intilization;
using TBDD.Models.MobileSink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TBDD.Models.Cell;
using TBDD.Region;
namespace TBDD.Constructor
{
    /// <summary>
    /// Interaction logic for Tree.xaml
    /// </summary>
    /// 
    public partial class Level
    {
        public List<CellGroup> nodes = new List<CellGroup>();
        public int nodesCount { get; set; }
        public int levelID { get; set; }
        Point currentPosition { get; set; }
        

    }
    public partial class Link
    {
        public List<CellGroup> hasLinkwith = new List<CellGroup>();

    }

    public partial class Tree : UserControl
    {
        //public static int sinkReg { get; set; }
        private static List<CellGroup> activeRegCells { get; set; }
        private static CellGroup rootCluster { get; set; }
        private static Queue<CellGroup> BFSvisited = new Queue<CellGroup>();
        private static Canvas sensingField;
        private static List<CellGroup> tempClusterGroup = new List<CellGroup>();
        //Variables that will contain manipulating the tree
        public List<CellGroup> clusterTree = new List<CellGroup>();
        public static int rootClusterID { get; set; }
        public static int OldrootClusterID { get; set; }
        public static Canvas stack_panel = new Canvas();
        //Showing the tree variables
        private static List<Level> clusterLevels = new List<Level>();
        private static List<Tree> treeNodes = new List<Tree>();

        private static Queue<CellGroup> TotalBFSvisited = new Queue<CellGroup>();
        public List<CellGroup> TotalclusterTree = new List<CellGroup>();
        private static List<Level> TotalclusterLevels = new List<Level>();


        private static DispatcherTimer timer = new DispatcherTimer();

        public Tree()
        {

        }

        public Tree(Canvas canv)
        {
            InitializeComponent();
            sensingField = canv;           
            buildTree();
            displayTree();
        }

        public Tree(int activeReg)
        {
            PublicParameters.currentNetworkTree.Clear(); // this get value in Startfromcluster function, hence should be cleared
            BFSvisited.Clear();
            clusterTree.Clear();
            clusterLevels.Clear();
            rootCluster = null;

            List<CellGroup> allcells = PublicParameters.networkCells;
            //activeRegCells = PublicParameters.listOfRegs[activeReg - 1].Cells;
            ClearSetLevel();

            Console.WriteLine(" \n ========== sink in region {0}, tree construction ========", PublicParameters.SinkNode.sReg);
            //buildTree(allcells);
            displayTree();

        }

        public static void ClearSetLevel()
        {
            foreach (CellGroup node in PublicParameters.networkCells)
            {
                node.childrenClusters.Clear();
                node.isLeafNode = false;
                node.parentCluster = null;
                node.clusterLevel = 0;
                node.isVisited = false;
                
            }

        }

        public void setLevel(CellGroup node)
        {
            if (node.childrenClusters.Count > 0)
            {
                foreach (CellGroup child in node.childrenClusters)
                {
                    child.clusterLevel = node.clusterLevel + 1;
                    setLevel(child);
                }
            }
        }


        private void saveLevels()
        {
            for (int i = 1; i <= PublicParameters.networkCells.Count(); i++)
            {
                Level level = new Level();
                level.levelID = i;
                foreach (CellGroup node in PublicParameters.networkCells)
                {
                    if (node.clusterLevel == level.levelID)
                    {
                        level.nodes.Add(node);
                    }

                }
                level.nodesCount = level.nodes.Count();
                if (level.nodesCount > 0)
                {
                    clusterLevels.Add(level);
                }

            }



        }

        public static void printLevels()
        {
            Console.WriteLine("Printiing");
            foreach (Level level in clusterLevels)
            {
                Console.WriteLine("** Level {0} has {1}", level.levelID, level.nodesCount);
                foreach (CellGroup node in level.nodes)
                {
                    Console.Write("   {0}", node.getID());
                    if (node.getID() != rootCluster.getID())
                    {
                        Console.Write("  with parent {0}", node.parentCluster.getID());
                    }
                }
                Console.WriteLine();
            }
        }
 
       
        public void displayTree()
        {
      
            CellGroup root = CellGroup.getClusterWithID(rootClusterID);
            
            root.clusterLevel = 1;
            setLevel(root);
            saveLevels();
            printLevels();
            //drawLines("add");

        }


        /*
         *  1- Start from cluster center or randomly select a number with the median as the mean
         *  2- Get the children and add their parents var, then add them to the cluster root's children list
         *  3- add the cluster to the tempClusterGroup 
         *  4- add the rootCluster to the visited list
         *  5- take variables from the visited list and search the children variable 
         *  6- if the clusters have children add them to BFS visited 
         *  7- else set them as leafNode untill BFSVisited is empty
         */
         

        private void startFromCluster(CellGroup parent, bool isRoot)
        {

            double radius = PublicParameters.cellDiameter;
            parent.isVisited = true;
            double offset = Math.Ceiling((radius + (radius / 2)));
            offset = Math.Sqrt(Math.Pow(offset, 2) + Math.Pow(offset, 2));
            if (isRoot)
            {
                parent.parentCluster = null;
                rootCluster = parent;
                rootClusterID = parent.getID();
                MobileModel.rootTreeID = rootClusterID;

            }

            PublicParameters.currentNetworkTree.Add(parent);

            foreach (CellGroup child in PublicParameters.networkCells)
            {

                if (!child.isVisited)
                {

                    double distance = Operations.DistanceBetweenTwoPoints(parent.clusterActualCenter, child.clusterActualCenter);
                    // Console.WriteLine("Distance between {0} and {1} is {2}",  parent.getID(), child.getID(),distance);
                    //Think about the lines where x difference might be 0 if no change and less than half the radius if it changed its place
                    //Also for Y
                    if (distance <= offset)
                    {

                        //That means this cluster is a child so we add it and edit it's parent variable
                        if (child.clusterReg == rootCluster.clusterReg && parent.clusterReg != rootCluster.clusterReg)
                        {
                            continue;
                        }
                        else 
                        {
                            child.parentCluster = parent;
                            child.isVisited = true;
                            parent.childrenClusters.Add(child);
                        }

                    }
                }

            }
            if (parent.childrenClusters.Count() > 0)
            {
                parent.isLeafNode = false;
            }
            else
            {
                parent.isLeafNode = true;
            }

            BFSvisited.Enqueue(parent);
        }

        public static int getNearToSinkCellId(List<CellGroup> regCells)
        {
            // ========== changing the rootcluster id which was by default 1 =======
            double distancefromsink = double.MaxValue;
            CellGroup firstroot = new CellGroup();

            foreach (CellGroup neartosink in regCells)
            {
                //if (neartosink.clusterReg == PublicParameters.SinkNode.sReg)
                //{
                    double distance = Operations.DistanceBetweenTwoPoints(PublicParameters.SinkNode.CenterLocation, neartosink.clusterActualCenter);
                    if (distance < distancefromsink)
                    {
                        distancefromsink = distance;
                        firstroot = neartosink;
                    }
                //}

            }
            return firstroot.getID();
            // =================================================
        }

        //private void buildTree()
        //{
        //    getClusterLinks();
        //}
        private void buildTree()
        {
            getClusterLinks();

            OldrootClusterID = rootClusterID;
            rootClusterID = getNearToSinkCellId(PublicParameters.networkCells);
            PublicParameters.SinkNode.sReg = CellGroup.getClusterWithID(rootClusterID).clusterReg;
            int clusterCount = PublicParameters.networkCells.Count();

            startFromCluster(CellGroup.getClusterWithID(rootClusterID), true);
            CellGroup.getClusterWithID(rootClusterID).CellTable.isRootCell = true;
            CellGroup.getClusterWithID(OldrootClusterID).CellTable.isRootCell = false;

            for (int i = 0; (BFSvisited.Count == 0) || (i < clusterCount - 1); i++)
            {
                if (BFSvisited.Count() > 0)
                {
                    CellGroup parent = BFSvisited.Dequeue();
                    Queue<CellGroup> childrenQue = new Queue<CellGroup>();// parent.childrenClusters;
                    foreach (CellGroup child in parent.childrenClusters)
                    {
                        childrenQue.Enqueue(child);
                    }
                    int childrenCount = childrenQue.Count();
                    for (int q = 0; q < childrenCount; q++)
                    {
                        startFromCluster(childrenQue.Dequeue(), false);
                    }


                }
                else
                {
                    break;
                }

            }


        }
        private static List<Line> connections = new List<Line>();
        public static void drawLines(string action)
        {
            if (action == "add")
            {
                foreach (Level level in clusterLevels)
                {
                    if (level.nodesCount > 0)
                    {
                        foreach (CellGroup node in level.nodes)
                        {
                            if (node.getID() != rootClusterID)
                            {
                                CellGroup parent = node.parentCluster;
                                Point parentPos = getNode(parent).treeNodePos;
                                node.treeParentNodePos = parentPos;
                            }


                        }
                    }

                }
                foreach (Level level in clusterLevels)
                {
                    if (level.nodesCount > 0)
                    {
                        foreach (CellGroup node in level.nodes)
                        {
                            if (node.getID() != rootClusterID)
                            {

                                //Point parentPos = node.treeParentNodePos;
                                //Point nodePos = node.treeNodePos;
                                //parentPos.X += 25 / 2;
                                //parentPos.Y += 25;
                                //nodePos.X += 25 / 2;


                                Line connection = new Line();
                                connection.Stroke = Brushes.Red;
                                connection.Fill = Brushes.Red;
                                connection.X1 = node.parentCluster.clusterActualCenter.X;
                                connection.Y1 = node.parentCluster.clusterActualCenter.Y;
                                connection.X2 = node.clusterActualCenter.X;
                                connection.Y2 = node.clusterActualCenter.Y;
                                //stack_panel.Children.Add(connection);
                                sensingField.Children.Add(connection);
                                connections.Add(connection);
                            }
                        }
                    }
                }
            }
            else if (action == "remove")
            {
                foreach (Line lin in connections)
                {
                    sensingField.Children.Remove(lin);
                }
            }

        }

        private void getClusterLinks()
        {
            double radius = PublicParameters.cellDiameter;
            double offset = Math.Ceiling((radius + (radius / 2)));
            offset = Math.Sqrt(Math.Pow(offset, 2) + Math.Pow(offset, 2));

            foreach (CellGroup clust in PublicParameters.networkCells)
            {
                
                foreach (CellGroup otherclust in PublicParameters.networkCells)
                {                    
                    if (clust.getID() != otherclust.getID())
                    {
                        double distance = Operations.DistanceBetweenTwoPoints(clust.clusterActualCenter, otherclust.clusterActualCenter);
                        if (distance <= offset)
                        {
                            clust.clusterLinks.hasLinkwith.Add(otherclust);

                        }
                    }
                }
                //if (clust.clusterLinks.hasLinkwith.Count > 0)
                //{
                //    Console.Write("\nCell {0} haslink with : ", clust.getID());
                //    foreach (CellGroup NeighCell in clust.clusterLinks.hasLinkwith)
                //    {
                //        Console.Write(" , {0}", NeighCell.getID());
                //    }
                //}
            }
        }

        private List<CellGroup> alreadyLinked = new List<CellGroup>();
        private void getClusterLinksForBigTree()
        {
            double radius = PublicParameters.cellDiameter;
            double offset = Math.Ceiling((radius + (radius / 2)));
            offset = Math.Sqrt(Math.Pow(offset, 2) + Math.Pow(offset, 2));

            foreach (CellGroup clust in PublicParameters.networkCells)
            {
                if (!isalreadylinked(clust))
                {
                    alreadyLinked.Add(clust);
                }
                foreach (CellGroup otherclust in PublicParameters.networkCells)
                {
                    if (clust.getID() != otherclust.getID())
                    {
                        if (!isalreadylinked(otherclust))
                        {
                            double distance = Operations.DistanceBetweenTwoPoints(clust.clusterActualCenter, otherclust.clusterActualCenter);
                            if (distance <= offset)
                            {
                                clust.clusterBeaconLinks.hasLinkwith.Add(otherclust);
                                otherclust.clusterBeaconLinks.hasLinkwith.Add(clust);
                                alreadyLinked.Add(otherclust);
                            }
                        }
                    }
                }

                if (clust.clusterBeaconLinks.hasLinkwith.Count > 0)
                {
                    Console.Write("\nCell {0} has beacon link with : ", clust.getID());
                    foreach (CellGroup NeighCell in clust.clusterBeaconLinks.hasLinkwith)
                    {
                        Console.Write(" , {0}", NeighCell.getID());
                    }
                }
            }
        }
        private bool isalreadylinked(CellGroup cell)
        {
            if (alreadyLinked.Count > 0)
            {
                foreach (CellGroup cls in alreadyLinked)
                {
                    if (cell.getID() == cls.getID())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void keepChanging(int nearest, int newCellReg)
        {          
            Tree tree = new Tree();
            changeTree(nearest);
            clusterLevels.Clear();
            treeNodes.Clear();
            //stack_panel.Children.RemoveRange(0, stack_panel.Children.Count);

            //drawLines("remove");
            tree.displayTree();
        }

        public void startChanging(Canvas stackPanel)
        {
            stack_panel = stackPanel;

            //timer.Tick += timer_tick_change;
            //keepChanging(nearest);

        }

        private static CellGroup getNode(CellGroup node)
        {
            CellGroup found = null;
            foreach (Level level in clusterLevels)
            {
                if (level.nodesCount > 0)
                {
                    foreach (CellGroup compareNode in level.nodes)
                    {
                        if (compareNode.getID() == node.getID())
                        {
                            found = compareNode;
                        }
                    }
                }
                else if (level.nodes[0].getID() == node.getID())
                {
                    found = level.nodes[0];
                }
            }
            return found;
        }

        public static void changeTree(int nearClusterID)
        {
            if (nearClusterID != rootClusterID)
            {
                // PublicParamerters.currentNetworkTree.clusterTree.Clear();
                // The near cluster will be come the new root 
                CellGroup oldRoot = CellGroup.getClusterWithID(rootClusterID);
                CellGroup newRoot = CellGroup.getClusterWithID(nearClusterID);
               // oldRoot.clusterHeader.headerSensor.ClusterHeader.SinkAgent = null;
                //Edit the old root cluster's children & parent
                oldRoot.parentCluster = newRoot;
                oldRoot.childrenClusters.Remove(newRoot);
                //Edit the new root cluster's children & parent
                newRoot.parentCluster = null;
                newRoot.childrenClusters.Add(oldRoot);
                rootCluster = newRoot;
                rootClusterID = newRoot.getID();
                MobileModel.rootTreeID = rootClusterID;
                changeRootChildren();
                //if (PublicParameters.listOfRegs[rootCluster.clusterReg - 1].Cells.Count > 4)
                //{
                //    maintainActiveRegionTree();
                //}
                
                //Here we need to send to all the new headers the new parametrs in it
                // oldRoot.clusterHeader.headerSensor.CellHeader.hasSinkPosition = false;
                // oldRoot.clusterHeader.headerSensor.CellHeader.isRootHeader = false;
                // oldRoot.clusterHeader.headerSensor.CellHeader.ClearBuffer();

                oldRoot.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.hasSinkPosition = false;
                oldRoot.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.isRootHeader = false;
                oldRoot.CellTable.CellHeader.ClearCellHeaderBuffer();

                newRoot.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.hasSinkPosition = false;
                newRoot.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.SinkAgent = null;
                newRoot.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.isRootHeader = true;
                //newRoot.CellTable.CellHeader.GenerateTreeChange(oldRoot.CellTable.CellHeader);

                CellFunctions.ChangeTreeLevels();
                //drawLines("remove");
                //drawLines("add");

            }
        }

        public static void changeRegion(int oldreg)
        {
            foreach (CellGroup oldregcells in PublicParameters.listOfRegs[oldreg - 1].Cells)
            {
                oldregcells.parentCluster = null;
                oldregcells.childrenClusters.Clear();
                oldregcells.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.hasSinkPosition = false;
                oldregcells.CellTable.CellHeader.TBDDNodeTable.CellHeaderTable.isRootHeader = false;
                
            }
        }

        private static void changeRootChildren()
        {
            CellGroup root = CellGroup.getClusterWithID(rootClusterID);

            foreach (CellGroup linked in root.clusterLinks.hasLinkwith)
            {
                if (linked.parentCluster.getID() != rootClusterID)
                {
                    double offset = PublicParameters.cellDiameter + PublicParameters.cellDiameter / 2;
                    double distance = Operations.DistanceBetweenTwoPoints(linked.clusterActualCenter, root.clusterActualCenter);

                    CellGroup oldParent = CellGroup.getClusterWithID(linked.parentCluster.getID());
                    CellGroup child = CellGroup.getClusterWithID(linked.getID());
                    oldParent.childrenClusters.Remove(linked);
                    child.parentCluster = root;
                    root.childrenClusters.Add(linked);
                }
            }
            
        }

        private static void maintainActiveRegionTree()
        {
            CellGroup root = CellGroup.getClusterWithID(rootClusterID);

            foreach (CellGroup child in PublicParameters.listOfRegs[root.clusterReg - 1].Cells)
            {
                double radius = PublicParameters.cellDiameter;
                double offset = Math.Ceiling((radius + (radius / 2)));
                offset = Math.Sqrt(Math.Pow(offset, 2) + Math.Pow(offset, 2));
                //if (child != root && child.parentCluster.clusterReg != child.clusterReg && !root.clusterLinks.hasLinkwith.Contains(child))
                if (child != root && !root.clusterLinks.hasLinkwith.Contains(child))
                {                    
                    double mindistancetoRoot = double.MaxValue;
                    CellGroup mynewparent = null;
                    foreach (CellGroup rootchild in root.clusterLinks.hasLinkwith)
                    {
                        double distancetoRoot = Operations.DistanceBetweenTwoPoints(rootchild.clusterActualCenter, root.clusterActualCenter);
                        double distancetome = Operations.DistanceBetweenTwoPoints(rootchild.clusterActualCenter, child.clusterActualCenter);
                        if (distancetoRoot + distancetome < mindistancetoRoot)
                        {
                            mynewparent = rootchild;
                            mindistancetoRoot = distancetoRoot + distancetome;
                        }
                    }
                    if (mynewparent != null)
                    {                       
                        child.parentCluster.childrenClusters.Remove(child);
                        child.parentCluster = mynewparent;
                        mynewparent.childrenClusters.Add(child);
                    }
                    else
                    {
                        child.parentCluster = root;
                        child.parentCluster.childrenClusters.Remove(child);
                        root.childrenClusters.Add(child);
                    }
                }

                if (child.childrenClusters.Count() > 0)
                {
                    child.isLeafNode = false;
                }
                else
                {
                    child.isLeafNode = true;
                }

            }           
        }

        private void _btn_cluster_change(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Grid parent = btn.Parent as Grid;
            Label text = parent.Children[1] as Label;
            String x = text.Content.ToString();
            int nearest;
            Int32.TryParse(x, out nearest);
            //keepChanging(nearest);
        }

        private Queue<CellGroup> TreeQueueReingfold = new Queue<CellGroup>();

        private void setInitialXVal(CellGroup parent)
        {
            int numberofChildren = parent.childrenClusters.Count;
            int startX = 0;
            if (numberofChildren > 0)
            {
                foreach (CellGroup child in parent.childrenClusters)
                {
                    child.xValue = startX;
                    startX++;
                    TreeQueueReingfold.Enqueue(child);
                }
            }
           
            if (TreeQueueReingfold.Count > 0)
            {
                setInitialXVal(TreeQueueReingfold.Dequeue());
            }

        }

        private void Reingfold_Tree_Drawing()
        {
            setInitialXVal(rootCluster);
        }



    }
}
