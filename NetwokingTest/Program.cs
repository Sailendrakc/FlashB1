using System;
using FlashPeer;
namespace NetwokingTest
{
    class Program
    {
        private static string fullkey = "<RSAKeyValue><Modulus>mgMp3fgUlldflNQY00sCvP9" +
            "DHy8zysSt4KdQFkPjGoAkzon9+QLdiTa2G57fC/29ahhTrsRRtPhhQVJyckl72Nm+/31ceD1bOvjVQ" +
            "F7vfTbnoyDJGuwu9DAcQwjUk6BIn8ScfVuFVFdRAxCPgrK3h/PGgDKP+wLD2U87La68WuTmOep1/BhVTVE" +
            "T7woa91ijTr4GUoLLSYJOH3+/MRYyIGZuDxTqXXD79XdqlsdxU0PsEuilKeKEk3uv3vE40eoQi6MrjEBW8xGI" +
            "1RpG3Ya3Cq6VsWMRU+xB4X8mY48Vh6pYbpoNWAVI5rGbnOeh7IwBgEiN6urllE26KGLtGUzUpQ==</Modulus>" +
            "<Exponent>AQAB</Exponent><P>yHAzAoWgw1GABJLrmdSoUZBOdhStgznI2iS5s2t01+cGCOlwFlZop5f9znCcferLA" +
            "IvmZd2xoI55ereb7WHHBgKA9/LlnY8UV0uYYRlzFavXr7YecO1OsCP/yq0YG8AuOJ805301H5vUU4hGwnEqbTd3jQRgLPmk" +
            "g9Pb38utT/8=</P><Q>xLRrL5vrQJYWNzVHxB8kdwsnAjHmC4eNfM4GWauNeLgllvCzOjBqW1WgY1CmyJkB1GRGjuBIniCEhLSQc" +
            "Gd/LM6zKKDWaa20+lVYYW3lyCIB34t3ovNc2xGWulMQzYmRJX7q0SBWSdvk7XrAaI1eJ+A5V62gOmCCqb79U5y+m1s=</Q><DP>DPCpa" +
            "mVEHrf5QtJVYYYII9PkLN1i4lDttZ+fWYT7cyDYE+U2Nkr30LIQUa6Ve8+XsX5Wrobke9AW6aRG7cldNvccamWFC2n8TzJzMPmao3CHqTFhv" +
            "7qiVN7OGcCZCNmcYk1s9fDwaA0AZTAsGUuDCLAHCNSafOzVASnBTS5yDvM=</DP><DQ>UshPN7kZt5Oyg8eDjXFBymvCHfVcCEwi6nxWRd" +
            "Sh9EUjZLOl6f5INGoD1ugxWMiz8WvGGgkf5pRu0N6gzv1vky7mTVnrAoydVqEmUdKLWr+dJDQwxD5BPNzZH08oCig0EqCoOByw0+KcJKl" +
            "9YkLkdkmyOEkU3pyRQNjlChQ0T3M=</DQ><InverseQ>SaV+snltT7hiWDrEXjYp/oKT2QG/296uQCNnfhdwPnt911+eNHUD4NO" +
            "h7eUykVfxD42loO35666o6teccbwlHcgDWIIgTP/+7x/AkZPOABymRj18QeB1kN+kdZuG201a7xavcxhPqT4E65smwOHgZv2B" +
            "8mZ3VqcGwJXjKhmxREY=</InverseQ><D>PYmKDwDy6Odcb5EXokVUgMPVw/4OSbSwbRUtMNhLQ+lzy3rjmb2FWzAbL" +
            "4oZQSdPqbayqSAULaUY5wrUY8nszEakxF0It5p8e2G1g5TrSDLJ9ypAcJtyX2thv38lwR7IJd5fUM9ixMJmwjy" +
            "5utVB8/Z5l/ucAAWoz4mS8bvh0scIbzQb4JU0aNtxRoOC3onhBsW0zBv5Kce3aY/mz/iloAcpIYpvl4tjV7" +
            "l5tgarPF4vRleMNHGIi7WU7SavBhLDo5x+27GyT/km5Za5I94J8P6FJeLYWla" +
            "/Jo06/wZyIwNipQlcFW5g0mw1d0py+sMva9M9cFpxuTZx8cYoBf63+Q==</D></RSAKeyValue>";

        static string halfkey = "<RSAKeyValue><Modulus>mgMp3fgUlldflNQY00sCvP9" +
            "DHy8zysSt4KdQFkPjGoAkzon9+QLdiTa2G57fC/29ahhTrsRRtPhhQVJyckl72Nm+/31ceD1bOvjVQ" +
            "F7vfTbnoyDJGuwu9DAcQwjUk6BIn8ScfVuFVFdRAxCPgrK3h/PGgDKP+wLD2U87La68WuTmOep1/BhVTVE" +
            "T7woa91ijTr4GUoLLSYJOH3+/MRYyIGZuDxTqXXD79XdqlsdxU0PsEuilKeKEk3uv3vE40eoQi6MrjEBW8xGI" +
            "1RpG3Ya3Cq6VsWMRU+xB4X8mY48Vh6pYbpoNWAVI5rGbnOeh7IwBgEiN6urllE26KGLtGUzUpQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        static FlashProtocol fp = null;
        static void Main(string[] args)
        {

            start(true);
            Console.ReadLine();
        }

        static void start(bool isserver)
        {
            if (isserver)
            {
                Console.WriteLine("Starting server");
                fp = new FlashProtocol(false, 5125, fullkey); 
                fp.StartPeer();
            }
            else
            {
                Console.WriteLine("Starting client");
                fp = new FlashProtocol(false, 5124, halfkey);
                fp.StartPeer();
                fp.StartHello(new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.0.13"), 5125));
            }
        }
    }
}
