//1.Given a binary search tree, return a balanced binary search tree with the same node values.
//A binary search tree is balanced if and only if the depth of the two subtrees of every node never differ by more than 1. If there is more than one answer, return any of them.
/*
        1
         \
          2
           \
            3
             \
              4
               \
                5

        3
       / \
      2   4
     /     \
    1       5
*/
public class Tree
{
    public Node Root
    public int Count

    public Tree(int firstValue)
    {
        Root = new Node(firstValue);
        Count++;
    }

    public void Add(int value)
    {
        AddHelper(Root, value);
        Count++;
    }

    private void AddHelper(Node node, int value)
    {
        int curVal = node.Value;
        if (value < curVal)
        {
            if (node.Left != null)
                AddHelper(node.Left);
            else
                node.Left = new Node(value);
        }
        else
        {
            if (node.Right != null)
                AddHelper(node.Right);
            else
                node.Right = new Node(value);
        }
    }

    public static Tree Balance(Node unbalancedRoot)
    {
        //iterate through unbalanceRoot to find our root value
        List<int> values = GetList(new List<int>(), unbalancedRoot);

        //We have a sorted list of the values
        //[1, 2, 3, 4] starts with 3
        //[1, 2, 3, 4, 5] starts with 3
        //[1, 2, 3, 4, 5, 6] start with 4
        int indexToGet = (values.Count / 2); //1
        int rootValue = values.ToArray()[indexToGet]; //2

        //construct the rest of the balanced tree
        Tree newTree = new Tree(rootValue);
        foreach (int val in values)
        {
            if (val != rootValue)
            {
                //add the val to the tree balanaced
                newTree.Add(val);
            }
        }
        return newTree;
    }

    public void Print(Node root)
    {
        if (root.Left == null && root.Right == null)
        {
            Console.WriteLine(root.Value);
        }
        else if (root.Left != null)
        {
            Print(root.Left);
        }
        else if (root.Right != null)
        {
            Print(root.Right);
        }
    }

    public List<int> GetList(List<int> values, Node root)
    {
        if (root.Left == null && root.Right == null)
        {
            values.Add(root.Value);
        }
        else if (root.Left != null)
        {
            GetList(root.Left);
        }
        else if (root.Right != null)
        {
            GetList(root.Right);
        }
    }
}

public class Node
{
    public int Value;
    public Node Left;
    public Node Right;

    public Node(int value)
    {
        Value = value;
        Left = null;
        Right = null;
    }
}
/*
Input: root = [1,null,2,null,3,null,4,null,null]
1
 \
  2
   \
    3
     \
      4
Output: [2,1,3,null,null,null,4]
        2
       / \
      1   3
           \
            4

Explanation: This is not the only correct answer, [3,1,4,null,2,null,null] is also correct.

Test with input
indexToGet = 1
rootValue = 2
Result Tree for first input data:
                    2
                   / \
                  1   3
                       \
                        4

Result Tree for second input data:

                   3
                  / \
                 1   4
                  \   \
                   2   5
*/
public class Test
{
    public static void Main(string[] args)
    {
        //GIVEN: this function gives us the root of the unbalanced tree
        Node unbalancedRoot = GetUnbalancedRoot();

        Tree newTree = Tree.Balance(unbalancedRoot);
    }
}
