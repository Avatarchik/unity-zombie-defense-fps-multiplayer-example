using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopDetector : MonoBehaviour {
	public Transform shootPoint;
	public float detectRange;
	public Text shopText;
	public Text warningText;
	public AudioClip purchasedSound;

	int healUsed = 0;
	NetworkPlayer networkPlayer;
	NetworkManager networkManager;
	NetworkPlayerStatus playerStatus;

	IEnumerator warningTextCo = null;

	IEnumerator HideWarningText() {
		yield return new WaitForSeconds(3f);

		warningText.text = "";
		yield break;
	}
	
	void Awake() {
		networkPlayer = transform.parent.GetComponent<NetworkPlayer>();
		networkManager = GameObject.Find("GameManager").GetComponent<NetworkManager>();
		playerStatus = GameObject.Find("GameManager").GetComponent<NetworkPlayerStatus>();
	}

	void Start() {
		shopText = GameObject.Find("UI/InGameUI/PlayerUI/Info/ShopText").GetComponent<Text>();
		warningText = GameObject.Find("UI/InGameUI/PlayerUI/Info/WarningText").GetComponent<Text>();
	}

	void PrintWarning(string text) {
		if(warningTextCo != null) StopCoroutine(warningTextCo);

		warningTextCo = HideWarningText();
		warningText.text = text;

		StartCoroutine(warningTextCo);
	}

	void BuyPrimaryWeapon(Weapon weapon) {
		string weaponName = weapon.ToString();
		WeaponManager weaponManager = transform.parent.gameObject.GetComponent<WeaponManager>();
		GameObject weaponGO = transform.Find("WeaponHolder/" + weaponName).gameObject;
		weaponManager.currentWeaponGO.GetComponent<WeaponBase>().Unload();

		weaponManager.currentWeapon = weapon;
		weaponManager.currentWeaponGO = weaponGO;

		weaponManager.primaryWeapon = weapon;
		weaponManager.primaryWeaponGO = weaponGO;

		WeaponBase weaponBase = weaponManager.currentWeaponGO.GetComponent<WeaponBase>();

		weaponManager.currentWeaponGO.SetActive(true);
		weaponBase.InitAmmo();
		weaponBase.Draw();

		networkPlayer.SwitchWeapon();
	}

	void BuySecondaryWeapon(Weapon weapon) {
		string weaponName = weapon.ToString();
		WeaponManager weaponManager = transform.parent.gameObject.GetComponent<WeaponManager>();
		GameObject weaponGO = transform.Find("WeaponHolder/" + weaponName).gameObject;
		weaponManager.currentWeaponGO.GetComponent<WeaponBase>().Unload();

		weaponManager.currentWeapon = weapon;
		weaponManager.currentWeaponGO = weaponGO;

		weaponManager.secondaryWeapon = weapon;
		weaponManager.secondaryWeaponGO = weaponGO;

		WeaponBase weaponBase = weaponManager.currentWeaponGO.GetComponent<WeaponBase>();

		weaponManager.currentWeaponGO.SetActive(true);
		weaponBase.InitAmmo();
		weaponBase.Draw();

		networkPlayer.SwitchWeapon();
	}

	void UpgradeWeapon(WeaponBase weaponBase, ShopType upgradeType) {
		switch(upgradeType) {
			case ShopType.UPGRADE_DAMAGE:
				weaponBase.upgradeDamage++;
				break;
			case ShopType.UPGRADE_RELOAD:
				weaponBase.upgradeReload++;
				break;
			case ShopType.UPGRADE_RECOIL:
				weaponBase.upgradeRecoil++;
				break;
			case ShopType.UPGRADE_MAGAZINE:
				weaponBase.upgradeMag++;
				weaponBase.RecalculateMagSize();
				break;
			case ShopType.UPGRADE_MAX_AMMO:
				weaponBase.upgradeMaxAmmo++;
				weaponBase.RecalculateMaxAmmo();
				break;
		}
	}

	int GetAmmoCost(Weapon weapon, WeaponBase weaponBase) {
		int cost = weaponBase.ammoBasicCost;
		float weaponAmmoCostFactor = weaponBase.ammoUpgradeCostFactor;

		int upgradeDamage = weaponBase.upgradeDamage;
		int upgradeMag = weaponBase.upgradeMag;
		int upgradeMaxAmmo = weaponBase.upgradeMaxAmmo;
		int upgradeRange = weaponBase.upgradeRange;
		int upgradeRecoil = weaponBase.upgradeRecoil;
		int upgradeReload = weaponBase.upgradeReload;
		int upgradeSteady = weaponBase.upgradeSteady;

		float upgradeCost = (upgradeDamage + upgradeMag + upgradeMaxAmmo + upgradeRange + upgradeRecoil + upgradeReload + upgradeSteady) * weaponAmmoCostFactor;
		cost = cost + (int)upgradeCost;

		return cost;
	}

	int GetUpgradeCost(Weapon weapon, WeaponBase weaponBase, int upgraded) {
		int basePrice = weaponBase.upgradeCost;

		return basePrice * (upgraded + 1);
	}

	int GetResurrectionCost() {
		int players = PhotonNetwork.playerList.Length;
		int alivePlayers = GetPlayerCount();
		int deadPlayers = players - alivePlayers;
		int resurrectionPrice = 100;

		return resurrectionPrice * deadPlayers;
	}

	int GetPlayerCount() {
		return GameObject.FindGameObjectsWithTag("Player").Length;
	}

	void Update() {
		RaycastHit hit;
		Vector3 position = shootPoint.position;
		position.y += 1;	// Adjust height differences

		if(Physics.Raycast(position, transform.TransformDirection(Vector3.forward * detectRange), out hit, detectRange)) {
			if(hit.transform.tag == "Shop") {
				Shop shop = hit.transform.GetComponent<Shop>();
				ShopType shopType = shop.shopType;
				string shopTitle = shop.title;
				string shopDesc = shop.description;
				int shopPrice = shop.price;
				bool isPurchasable = true;

				WeaponManager weaponManager = transform.parent.gameObject.GetComponent<WeaponManager>();
				WeaponBase weaponBase = weaponManager.currentWeaponGO.GetComponent<WeaponBase>();
				Weapon weapon = weaponManager.currentWeapon;
				Player player = transform.parent.GetComponent<Player>();

				if(shopType == ShopType.AMMO) {
					shopPrice = GetAmmoCost(weapon, weaponBase);
					shopText.text = shopTitle + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
				}
				else if(shopType == ShopType.HEAL) {
					shopPrice = 100 + (75 * healUsed);
					shopText.text = shopTitle + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
				}
				else if(shopType == ShopType.RESURRECTION) {
					shopPrice = GetResurrectionCost();
					shopText.text = shopTitle + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
				}
				else if(shopType == ShopType.UPGRADE_HEALTH) {
					int upgraded = player.upgradeHealth;

					if(upgraded < 10) {
						shopPrice = 100 + (upgraded * 100);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "You are fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_REGENERATION) {
					int upgraded = player.upgradeRegeneration;

					if(upgraded < 10) {
						shopPrice = 100 + (upgraded * 75);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "You are fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_DAMAGE) {
					int upgraded = weaponBase.upgradeDamage;

					if(upgraded < 10) {
						shopPrice = GetUpgradeCost(weaponManager.currentWeapon, weaponBase, upgraded);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "Your weapon is fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_RELOAD) {
					int upgraded = weaponBase.upgradeReload;

					if(upgraded < 10) {
						shopPrice = GetUpgradeCost(weaponManager.currentWeapon, weaponBase, upgraded);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "Your weapon is fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_RECOIL) {
					int upgraded = weaponBase.upgradeRecoil;

					if(upgraded < 10) {
						shopPrice = GetUpgradeCost(weaponManager.currentWeapon, weaponBase, upgraded);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "Your weapon is fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_MAGAZINE) {
					int upgraded = weaponBase.upgradeMag;

					if(upgraded < 10) {
						shopPrice = GetUpgradeCost(weaponManager.currentWeapon, weaponBase, upgraded);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "Your weapon is fully upgraded.";
					}
				}
				else if(shopType == ShopType.UPGRADE_MAX_AMMO) {
					int upgraded = weaponBase.upgradeMaxAmmo;

					if(upgraded < 10) {
						shopPrice = GetUpgradeCost(weaponManager.currentWeapon, weaponBase, upgraded);
						shopText.text = shopTitle + " Lv" + (upgraded + 1) + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
					}
					else {
						isPurchasable = false;
						shopText.text = "Your weapon is fully upgraded.";
					}
				}
				else {
					shopText.text = shopTitle + "\n(" + shopPrice + "$)\n\n" +  shopDesc + "\n\n";
				}

				if(isPurchasable && Input.GetKeyDown(KeyCode.F)) {
					FundSystem fundSystem = transform.parent.GetComponent<FundSystem>();
					int fund = fundSystem.GetFund();

					if(fund < shopPrice) {
						PrintWarning("Not enough money!");
					}
					else {
						bool wasPurchased = true;

						if(shopType == ShopType.AMMO) {
							if(weaponBase.bulletsLeft >= (weaponBase.maxAmmo + weaponBase.bulletsPerMag)) {
								wasPurchased = false;
								PrintWarning("You have full ammo.");
							}
							else {
								weaponBase.bulletsLeft = weaponBase.maxAmmo + weaponBase.bulletsPerMag;
								weaponBase.UpdateAmmoText();
							}
						}
						else if(shopType == ShopType.HEAL) {
							HealthManager healthManager = transform.parent.GetComponent<HealthManager>();

							if(healthManager.Health >= healthManager.MaxHealth) {
								wasPurchased = false;
								PrintWarning("You have full health.");
							}
							else {
								healthManager.Heal();
								healUsed++;
							}
						}
						else if(shopType == ShopType.RESURRECTION) {
							if(GetResurrectionCost() <= 0) {
								wasPurchased = false;
								PrintWarning("No players to revive.");
							}
							else {
								networkManager.RevivePlayers();
							}
						}
						else if(shopType == ShopType.UPGRADE_HEALTH) {
							HealthManager healthManager = transform.parent.GetComponent<HealthManager>();
							float addtionalHealth = 20f;
							
							healthManager.SetHealth(healthManager.Health + addtionalHealth);
							healthManager.SetMaxHealth(healthManager.MaxHealth + addtionalHealth);

							player.upgradeHealth++;
						}
						else if(shopType == ShopType.UPGRADE_REGENERATION) {
							if(player.upgradeRegeneration <= 0) {
								player.ActivateHealthRegeneration();
							}

							player.upgradeRegeneration++;
						}
						else if(shopType == ShopType.WEAPON_MP5K) {
							if(!weaponManager.HasWeapon(Weapon.MP5K)) {
								BuyPrimaryWeapon(Weapon.MP5K);
							}
							else {
								wasPurchased = false;
								PrintWarning("You already have weapon.");
							}
						}
						else if(shopType == ShopType.WEAPON_PYTHON) {
							if(!weaponManager.HasWeapon(Weapon.Python)) {
								BuySecondaryWeapon(Weapon.Python);
							}
							else {
								wasPurchased = false;
								PrintWarning("You already have weapon.");
							}
						}
						else if(shopType == ShopType.WEAPON_UMP45) {
							if(!weaponManager.HasWeapon(Weapon.UMP45)) {
								BuyPrimaryWeapon(Weapon.UMP45);
							}
							else {
								wasPurchased = false;
								PrintWarning("You already have weapon.");
							}
						}
						else if(shopType == ShopType.WEAPON_AKM) {
							if(!weaponManager.HasWeapon(Weapon.AKM)) {
								BuyPrimaryWeapon(Weapon.AKM);
							}
							else {
								wasPurchased = false;
								PrintWarning("You already have weapon.");
							}
						}
						else if(shopType == ShopType.WEAPON_M870) {
							if(!weaponManager.HasWeapon(Weapon.M870)) {
								BuyPrimaryWeapon(Weapon.M870);
							}
							else {
								wasPurchased = false;
								PrintWarning("You already have weapon.");
							}
						}
						else if(shopType == ShopType.UPGRADE_DAMAGE) {
							if(weaponBase.upgradeDamage >= 10) {
								wasPurchased = false;
								PrintWarning("Your weapon is fully upgraded.");
							}
							else {
								UpgradeWeapon(weaponBase, ShopType.UPGRADE_DAMAGE);								
							}
						}
						else if(shopType == ShopType.UPGRADE_RELOAD) {
							if(weaponBase.upgradeReload >= 10) {
								wasPurchased = false;
								PrintWarning("Your weapon is fully upgraded.");
							}
							else {
								UpgradeWeapon(weaponBase, ShopType.UPGRADE_RELOAD);								
							}
						}
						else if(shopType == ShopType.UPGRADE_RECOIL) {
							if(weaponBase.upgradeRecoil >= 10) {
								wasPurchased = false;
								PrintWarning("Your weapon is fully upgraded.");
							}
							else {
								UpgradeWeapon(weaponBase, ShopType.UPGRADE_RECOIL);								
							}
						}
						else if(shopType == ShopType.UPGRADE_MAGAZINE) {
							if(weaponBase.upgradeMag >= 10) {
								wasPurchased = false;
								PrintWarning("Your weapon is fully upgraded.");
							}
							else {
								UpgradeWeapon(weaponBase, ShopType.UPGRADE_MAGAZINE);								
							}
						}
						else if(shopType == ShopType.UPGRADE_MAX_AMMO) {
							if(weaponBase.upgradeMaxAmmo >= 10) {
								wasPurchased = false;
								PrintWarning("Your weapon is fully upgraded.");
							}
							else {
								UpgradeWeapon(weaponBase, ShopType.UPGRADE_MAX_AMMO);								
							}
						}
						else {
							wasPurchased = false;
						}

						if(wasPurchased) {
							fundSystem.TakeFund(shopPrice);
							SoundManager soundManager = transform.Find("SoundManager").GetComponent<SoundManager>();
							soundManager.Play(purchasedSound);

							playerStatus.spentCash += shopPrice;
						}
					}
				}
			}
		}
		else {
			shopText.text = "";
		}
	}
}
