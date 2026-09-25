using System.Linq;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class DepthMechanismTests
    {
        [Test] public void AllTwentyFourChallengesHaveAValidSolutionAndReset()
        {
            for(int kind=0;kind<8;kind++)for(int stage=0;stage<3;stage++)
            {
                var p=new DepthMechanism(kind,stage);Assert.False(p.Solved);
                switch(kind)
                {
                    case 0:for(int i=0;i<3;i++)while(p.Cells[i]!=p.Goal[i])p.Tap(i);break;
                    case 1:foreach(int bell in p.Goal.Take(4+stage))p.Tap(bell);break;
                    case 2:foreach(int cell in stage==2?new[]{1,2,5,8}:new[]{3,6,7,8})p.Tap(cell);break;
                    case 3:p.Tap(0);p.Tap(4);p.Tap(8-stage);break;
                    case 4:foreach(int cell in new[]{1,2,5,4,3,6,7,8})p.Tap(cell);break;
                    case 5:p.Tap(0);p.Tap(0);p.Tap(1);p.Tap(1);break;
                    case 6:p.Tap(3);p.Tap(8-stage);p.Tap(2);p.Tap(7);p.Tap(0);p.Tap(5);break;
                    case 7:for(int i=0;i<3;i++)while(p.Cells[i*3]!=p.Cells[i*3+2])p.Tap(i*3+2);break;
                }
                Assert.True(p.Solved,$"kind {kind} stage {stage}");p.Reset();Assert.False(p.Solved);
            }
        }
    }
}
