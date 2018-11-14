using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;

namespace LivelyPets
{
  class LivelyCat : LivelyPet
  {
    public LivelyCat()
    {
      Sprite = new AnimatedSprite("Animals\\cat", 0, 32, 32);
      base.HideShadow = true;
      base.Breather = false;
      willDestroyObjectsUnderfoot = false;
    }

    public LivelyCat(Pet pet)
    {
      Sprite = new AnimatedSprite("Animals\\cat", 0, 32, 32);
      base.HideShadow = true;
      base.Breather = false;
      willDestroyObjectsUnderfoot = false;
    }


    public LivelyCat(int xTile, int yTile)
    {
      base.Name = "Cat";
      base.displayName = name;
      Sprite = new AnimatedSprite("Animals\\cat", 0, 32, 32);
      base.Position = new Vector2((float) xTile, (float) yTile) * 64f;
      base.Breather = false;
      willDestroyObjectsUnderfoot = false;
      base.currentLocation = Game1.currentLocation;
      base.HideShadow = true;
    }

    public override void initiateCurrentBehavior()
    {
      base.initiateCurrentBehavior();
    }

    public override void update(GameTime time, GameLocation location)
    {
      base.update(time, location);
      if (base.currentLocation == null)
      {
        base.currentLocation = location;
      }

      if (!Game1.eventUp && !Game1.IsClient)
      {
        if (this.Position.X < Game1.player.Position.X)
        {
          Position = new Vector2(Position.X + 1, Position.Y);
        }
      }
    }

    protected override void updateSlaveAnimation(GameTime time)
    {
      switch (base.CurrentBehavior)
      {
        case 1:
          if (Game1.random.NextDouble() < 0.002)
          {
            doEmote(24, true);
          }

          break;
        case 2:
          if (Sprite.currentFrame == 18 && Game1.random.NextDouble() < 0.01)
          {
            switch (Game1.random.Next(10))
            {
              case 0:
                break;
              case 1:
              case 2:
              case 3:
              {
                List<FarmerSprite.AnimationFrame> licks = new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(19, 300),
                  new FarmerSprite.AnimationFrame(20, 200),
                  new FarmerSprite.AnimationFrame(21, 200),
                  new FarmerSprite.AnimationFrame(22, 200, false, false, lickSound, false),
                  new FarmerSprite.AnimationFrame(23, 200)
                };
                int extraLicks = Game1.random.Next(1, 6);
                for (int i = 0; i < extraLicks; i++)
                {
                  licks.Add(new FarmerSprite.AnimationFrame(21, 150));
                  licks.Add(new FarmerSprite.AnimationFrame(22, 150, false, false, lickSound, false));
                  licks.Add(new FarmerSprite.AnimationFrame(23, 150));
                }

                licks.Add(new FarmerSprite.AnimationFrame(18, 1, false, false, base.hold, false));
                Sprite.loop = false;
                Sprite.setCurrentAnimation(licks);
                break;
              }
              default:
              {
                bool blink = Game1.random.NextDouble() < 0.45;
                Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(19, blink ? 200 : Game1.random.Next(1000, 9000)),
                  new FarmerSprite.AnimationFrame(18, 1, false, false, base.hold, false)
                });
                Sprite.loop = false;
                if (blink && Game1.random.NextDouble() < 0.2)
                {
                  playContentSound();
                  shake(200);
                }

                break;
              }
            }
          }

          break;
        case 0:
          faceDirection(base.FacingDirection);
          if (isMoving())
          {
            animateInFacingDirection(time);
          }
          else
          {
            Sprite.StopAnimation();
          }

          break;
      }
    }

    public void lickSound(Farmer who)
    {
      if (Utility.isOnScreen(getTileLocationPoint(), 128, base.currentLocation))
      {
        Game1.playSound("Cowboy_Footstep");
      }
    }

    public void leap(Farmer who)
    {
      if (base.currentLocation.Equals(Game1.currentLocation))
      {
        jump();
      }

      if (base.FacingDirection == 1)
      {
        xVelocity = 8f;
      }
      else if (base.FacingDirection == 3)
      {
        xVelocity = -8f;
      }
    }

    public void flopSound(Farmer who)
    {
      if (Utility.isOnScreen(getTileLocationPoint(), 128, base.currentLocation))
      {
        Game1.playSound("thudStep");
      }
    }

    public override void playContentSound()
    {
      if (Utility.isOnScreen(getTileLocationPoint(), 128, base.currentLocation))
      {
        Game1.playSound("cat");
      }
    }
  }
}
