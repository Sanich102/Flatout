﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flatout
{
    /// <summary>
    /// Основа машинки
    /// </summary>
    public abstract class CarBase : MonoBehaviour
    {
        private int _health;
        private float _booster;
        /// <summary>
        /// Максимальное количествово здоровья
        /// </summary>
        protected int MaxHealth;
        /// <summary>
        /// Текущее количествово здоровья
        /// </summary>
        protected int Health
        {
            get => _health;
            set
            {
                _health = Mathf.Clamp(value, 0, MaxHealth);
                OnHealthChanged?.Invoke(Health, MaxHealth);
            }
        }
        /// <summary>
        /// Максимальное количество заряда бустера
        /// </summary>
        float MaxBooster;
        /// <summary>
        /// Текущий заряд бустера
        /// </summary>
        float Booster
        {
            get => _booster;
            set
            {
                _booster = Mathf.Clamp(value, 0, MaxBooster);
                OnBoosterChanged?.Invoke(_booster, MaxBooster);
            }
        }
        /// <summary>
        /// Урон, наносимый при столкновении
        /// </summary>
        protected float speed;

        /// <summary>
        /// Урон, наносимый при столкновении
        /// </summary>
        protected int collisionDamage;
        /// <summary>
        /// <see cref="GameObject"/> компонент машинки
        /// </summary>
        public GameObject GameObj;
        /// <summary>
        /// Событие смерти машинки
        /// </summary>
        public Action<CarBase> OnDeath;
        /// <summary>
        /// Событие изменения здоровья машинки
        /// Первый параметр - количество здоровья, второй - максимальное здоровье
        /// </summary>
        public Action<int, int> OnHealthChanged;
        /// <summary>
        /// Событие изменения бустера машинки
        /// Первый параметр - количество бустера, второй - максимальный бустер
        /// </summary>
        public Action<float, float> OnBoosterChanged;
        /// <summary>
        /// Инициализация машинки
        /// </summary>
        /// <param name="carTier">Конфигурация машинки - отсюда берутся все ее стартовые характеристики</param>
        /// <param name="gameObj"><see cref="GameObject"/> компонент машинки</param>

        public int BoxesCrashed { get; private set; }
        public int CarsCrashed { get; private set; }
        public int XP { get; private set; }

        public void AddXP(int xp) => XP += xp;
        public void Init(CarTier carTier, GameObject gameObj)
        {
            MaxHealth = carTier.MaxHealth;
            Health = MaxHealth;
            MaxBooster = carTier.BoosterAmount;
            Booster = MaxBooster;
            GameObj = gameObj;
            collisionDamage = carTier.Damage;
            speed *= carTier.MovingSpeed;
            foreach (var damageZone in GetComponentsInChildren<TriggerCollider>())
            {
                damageZone.OnTriggered += DamageOtherCar;
            }
            SetCarSkin(GetCarColor(carTier));
        }
        /// <summary>
        /// Нанесение урона другой машинке
        /// </summary>
        /// <param name="otherCarCollider"><see cref="Collider"/> машинки</param>
        void DamageOtherCar(Collider otherCarCollider) => DamageOtherCar(otherCarCollider.gameObject.GetComponentInParent<CarBase>());
        /// <summary>
        /// Нанесение урона другой машинке
        /// </summary>
        /// <param name="otherCar">Управляющий компонент машинки</param>
        void DamageOtherCar(CarBase otherCar)
        {
            otherCar?.TakeDamage(collisionDamage, GameObj);
        }
        /// <summary>
        /// Получение урона
        /// </summary>
        /// <param name="damage">Количество полученного урона</param>
        public void TakeDamage(int damage, GameObject damager)
        {
            Health = Mathf.Max(0, Health - damage);
            if (Health == 0)
            {
                KillImmediately(damager);
            }
        }
        /// <summary>
        /// Возвращает никнейм машинки
        /// </summary>
        /// <returns>Никнейм</returns>
        public abstract string GetCarNickName();

        /// <summary>
        /// Возвращает текстуру-скин машинки
        /// </summary>
        /// <returns>Скин машинки</returns>
        public abstract Texture GetCarColor(CarTier carTier);

        /// <summary>
        /// Красит машинку в нужный скин
        /// </summary>
        /// <param name="texture">Текстура скина</param>
        void SetCarSkin(Texture texture)
        {
            foreach (var renderer in GameObj.GetComponentsInChildren<Renderer>())
            {
                if (renderer.gameObject.name.ToLower().Contains("wheel")) continue;
                renderer.material.SetTexture(1, texture);
            }
        }

        /// <summary>
        /// Смерть
        /// </summary>
        public void KillImmediately(GameObject killer)
        {
            OnDeath?.Invoke(this);
            transform.rotation = Quaternion.identity;
            GetComponentInChildren<Animator>().SetTrigger("Death");
            GetComponentsInChildren<Collider>().ToList().ForEach(x => x.gameObject.layer = 9);
            CarBase CarKiller;
            if (killer.TryGetComponent<CarBase>(out CarKiller))
            {
                CarKiller.CarCrashed();
            }
        }

        public void CarCrashed()
        {
            CarsCrashed++;
            AddXP(PlayerAvatar.Instance.hardnessLevel.XPForCarCrash);
        }
        public void BoxCrashed()
        {
            BoxesCrashed++;
            AddXP(PlayerAvatar.Instance.hardnessLevel.XPForBoxCrash);
        }

        /// <summary>
        /// Пытается взять заданное количество заряда ускорения
        /// </summary>
        /// <param name="boosterAmount">Максимально количество заряда, которое берется за эту команду</param>
        /// <returns>Количество реально взятого бустера (0 если заряд пустой)</returns>
        public float TakeBooster(float boosterAmount)
        {
            var receivedBooster = Booster > boosterAmount ? boosterAmount : Booster;
            Booster -= boosterAmount;
            return receivedBooster;
        }
    }

}
