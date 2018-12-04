using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

// Basically just game code with some AI additions
// In the future, much better off rewriting everything
// instead of using game code
namespace LivelyPets
{
  class LivelyCat : LivelyPet
  {
    public LivelyCat(Cat pet, IMonitor monitor)
    {
      base.Monitor = monitor;
      var petType = pet.GetType();
      PropertyInfo[] properties = petType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
      FieldInfo[] fields = petType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

      foreach (PropertyInfo property in properties)
      {
        try
        {
          property.SetValue(pet, property.GetValue(pet, null), null);
        }
        catch (ArgumentException) { } // For Get-only-properties
      }
      foreach (FieldInfo field in fields)
      {
        field.SetValue(this, field.GetValue(pet));
      }
    }

    public override void update(GameTime time, GameLocation location)
    {
      base.update(time, location);
      if (!isNearFarmer) return;
      
      if (base.currentLocation == null)
      {
        base.currentLocation = location;
      }
      if (!Game1.eventUp && !Game1.IsClient)
      {
        if (Game1.timeOfDay > 2000 && Sprite.CurrentAnimation == null && xVelocity == 0f && yVelocity == 0f)
        {
          base.CurrentBehavior = 1;
        }
        switch (base.CurrentBehavior)
        {
          case 1:
            if (Game1.timeOfDay < 2000 && Game1.random.NextDouble() < 0.001)
            {
              base.CurrentBehavior = 0;
            }
            else if (Game1.random.NextDouble() < 0.002)
            {
              doEmote(24, true);
            }
            return;
          case 2:
            if (Sprite.currentFrame != 18 && Sprite.CurrentAnimation == null)
            {
              initiateCurrentBehavior();
            }
            else if (Sprite.currentFrame == 18 && Game1.random.NextDouble() < 0.01)
            {
              switch (Game1.random.Next(10))
              {
                case 0:
                  base.CurrentBehavior = 0;
                  Halt();
                  faceDirection(2);
                  Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
            {
              new FarmerSprite.AnimationFrame(17, 200),
              new FarmerSprite.AnimationFrame(16, 200),
              new FarmerSprite.AnimationFrame(0, 200)
            });
                  Sprite.loop = false;
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
            if (Sprite.CurrentAnimation == null && Game1.random.NextDouble() < 0.01)
            {
              switch (Game1.random.Next(4))
              {
                case 0:
                case 1:
                case 2:
                  initiateCurrentBehavior();
                  break;
                case 3:
                  switch (base.FacingDirection)
                  {
                    case 0:
                    case 2:
                      Halt();
                      faceDirection(2);
                      Sprite.loop = false;
                      base.CurrentBehavior = 2;
                      break;
                    case 1:
                      if (Game1.random.NextDouble() < 0.85)
                      {
                        Sprite.loop = false;
                        Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(24, 100),
                  new FarmerSprite.AnimationFrame(25, 100),
                  new FarmerSprite.AnimationFrame(26, 100),
                  new FarmerSprite.AnimationFrame(27, Game1.random.Next(8000, 30000), false, false, flopSound, false)
                });
                      }
                      else
                      {
                        Sprite.loop = false;
                        Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(30, 300),
                  new FarmerSprite.AnimationFrame(31, 300),
                  new FarmerSprite.AnimationFrame(30, 300),
                  new FarmerSprite.AnimationFrame(31, 300),
                  new FarmerSprite.AnimationFrame(30, 300),
                  new FarmerSprite.AnimationFrame(31, 500),
                  new FarmerSprite.AnimationFrame(24, 800, false, false, leap, false),
                  new FarmerSprite.AnimationFrame(4, 1)
                });
                      }
                      break;
                    case 3:
                      if (Game1.random.NextDouble() < 0.85)
                      {
                        Sprite.loop = false;
                        Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(24, 100, false, true, null, false),
                  new FarmerSprite.AnimationFrame(25, 100, false, true, null, false),
                  new FarmerSprite.AnimationFrame(26, 100, false, true, null, false),
                  new FarmerSprite.AnimationFrame(27, Game1.random.Next(8000, 30000), false, true, flopSound, false),
                  new FarmerSprite.AnimationFrame(12, 1)
                });
                      }
                      else
                      {
                        Sprite.loop = false;
                        Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                {
                  new FarmerSprite.AnimationFrame(30, 300, false, true, null, false),
                  new FarmerSprite.AnimationFrame(31, 300, false, true, null, false),
                  new FarmerSprite.AnimationFrame(30, 300, false, true, null, false),
                  new FarmerSprite.AnimationFrame(31, 300, false, true, null, false),
                  new FarmerSprite.AnimationFrame(30, 300, false, true, null, false),
                  new FarmerSprite.AnimationFrame(31, 500, false, true, null, false),
                  new FarmerSprite.AnimationFrame(24, 800, false, true, leap, false),
                  new FarmerSprite.AnimationFrame(12, 1)
                });
                      }
                      break;
                  }
                  break;
              }
            }
            break;
        }
        if (Sprite.CurrentAnimation != null)
        {
          Sprite.loop = false;
        }
        if (Sprite.CurrentAnimation == null)
        {
          MovePosition(time, Game1.viewport, location);
        }
        else if (xVelocity != 0f || yVelocity != 0f)
        {
          Rectangle nextPosition = GetBoundingBox();
          nextPosition.X += (int)xVelocity;
          nextPosition.Y -= (int)yVelocity;
          if (base.currentLocation == null || !base.currentLocation.isCollidingPosition(nextPosition, Game1.viewport, false, 0, false, this))
          {
            position.X += (float)(int)xVelocity;
            position.Y -= (float)(int)yVelocity;
          }
          xVelocity = (float)(int)(xVelocity - xVelocity / 4f);
          yVelocity = (float)(int)(yVelocity - yVelocity / 4f);
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
