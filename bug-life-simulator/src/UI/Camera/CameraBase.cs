using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public abstract class CameraBase : ICamera
    {
        // === ПРИВАТНЫЕ ПОЛЯ (полная инкапсуляция) ===
        private Vector3 _position;
        private Vector3 _target;
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;
        private Matrix _viewProjectionMatrix;

        private int _viewportWidth;
        private int _viewportHeight;

        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;

        // === ПУБЛИЧНЫЕ СВОЙСТВА (только чтение) ===
        public Vector3 Position => _position;
        public Vector3 Target => _target;
        public Matrix ViewMatrix => _viewMatrix;
        public Matrix ProjectionMatrix => _projectionMatrix;
        public Matrix ViewProjectionMatrix => _viewProjectionMatrix;

        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;

        // === ЗАЩИЩЕННЫЕ СЕТТЕРЫ (контролируемое изменение) ===

        /// <summary>Установить позицию с проверками</summary>
        protected void SetPosition(Vector3 position, bool updateView = true)
        {
            if (_position == position) return;

            _position = position;

            if (updateView)
            {
                UpdateViewMatrix();
                UpdateViewProjection();
            }

            OnPositionChanged();
        }

        /// <summary>Установить цель с проверками</summary>
        protected void SetTarget(Vector3 target, bool updateView = true)
        {
            if (_target == target) return;

            _target = target;

            if (updateView)
            {
                UpdateViewMatrix();
                UpdateViewProjection();
            }

            OnTargetChanged();
        }

        /// <summary>Установить матрицу проекции</summary>
        protected void SetProjectionMatrix(Matrix matrix)
        {
            if (_projectionMatrix == matrix) return;

            _projectionMatrix = matrix;
            UpdateViewProjection();

            OnProjectionChanged();
        }

        /// <summary>Установить матрицу вида</summary>
        protected void SetViewMatrix(Matrix matrix)
        {
            if (_viewMatrix == matrix) return;

            _viewMatrix = matrix;
            UpdateViewProjection();

            OnViewChanged();
        }

        /// <summary>Обновить объединенную матрицу</summary>
        private void UpdateViewProjection()
        {
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
            OnViewProjectionChanged();
        }

        // === СВОЙСТВА ДЛЯ НАСЛЕДНИКОВ (контролируемые) ===

        /// <summary>Скорость движения (наследники могут менять, но с валидацией)</summary>
        protected float MoveSpeed { get; set; } = 5.0f;

        /// <summary>Скорость зума</summary>
        protected float ZoomSpeed { get; set; } = 0.1f;

        /// <summary>Скорость вращения</summary>
        protected float RotationSpeed { get; set; } = 0.005f;

        /// <summary>Использовать плавность движения</summary>
        protected bool UseSmoothing { get; set; } = true;

        /// <summary>Коэффициент плавности (0-1)</summary>
        protected float Smoothness { get; set; } = 0.85f;

        public int UpdateOrder => throw new NotImplementedException();

        public float DrawDepth => throw new NotImplementedException();

        public bool Visible => throw new NotImplementedException();

        // === КОНСТРУКТОР ===
        protected CameraBase(int viewportWidth, int viewportHeight)
        {
            if (viewportWidth <= 0 || viewportHeight <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;

            InitializeMatrices();
        }

        // === АБСТРАКТНЫЕ МЕТОДЫ ===
        public abstract void Update(GameTime gameTime);
        public abstract Vector2 WorldToScreen(Vector3 worldPosition);
        public abstract Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0);

        // === ВИРТУАЛЬНЫЕ МЕТОДЫ (можно переопределить) ===

        /// <summary>Инициализация матриц</summary>
        protected virtual void InitializeMatrices()
        {
            // Базовая ортографическая проекция
            SetProjectionMatrix(Matrix.CreateOrthographicOffCenter(
                0, _viewportWidth,
                _viewportHeight, 0,
                -1000, 1000));

            SetViewMatrix(Matrix.Identity);
            SetPosition(Vector3.Zero);
            SetTarget(Vector3.UnitZ);
        }

        /// <summary>Обновление матрицы вида (переопределить в наследниках)</summary>
        protected virtual void UpdateViewMatrix()
        {
            // Базовая реализация - камера смотрит на цель
            var viewMatrix = Matrix.CreateLookAt(
                _position,
                _target,
                Vector3.Up);

            SetViewMatrix(viewMatrix);
        }

        /// <summary>Установка размера viewport</summary>
        public virtual void SetViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = width;
            _viewportHeight = height;

            // Пересчитываем проекционную матрицу
            SetProjectionMatrix(Matrix.CreateOrthographicOffCenter(
                0, width, height, 0, -1000, 1000));
        }

        /// <summary>Направить камеру на цель (публичный метод)</summary>
        public virtual void LookAt(Vector3 target)
        {
            SetTarget(target);
            UpdateViewMatrix();
        }

        /// <summary>Переместить камеру (публичный метод)</summary>
        public virtual void Move(Vector3 offset)
        {
            if (offset == Vector3.Zero) return;

            SetPosition(_position + offset);
            SetTarget(_target + offset);
        }

        /// <summary>Установить позицию камеры (публичный метод)</summary>
        public virtual void Teleport(Vector3 position)
        {
            Vector3 offset = position - _position;
            SetPosition(position);
            SetTarget(_target + offset);
        }

        // === СОБЫТИЯ ДЛЯ НАСЛЕДНИКОВ ===

        /// <summary>Вызывается при изменении позиции</summary>
        protected virtual void OnPositionChanged() { }

        /// <summary>Вызывается при изменении цели</summary>
        protected virtual void OnTargetChanged() { }

        /// <summary>Вызывается при изменении матрицы вида</summary>
        protected virtual void OnViewChanged() { }

        /// <summary>Вызывается при изменении матрицы проекции</summary>
        protected virtual void OnProjectionChanged() { }

        /// <summary>Вызывается при изменении объединенной матрицы</summary>
        protected virtual void OnViewProjectionChanged() { }

        // === IDrawable реализация ===
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            // Камера по умолчанию не рисуется
            // Можно переопределить для отладочной визуализации
        }

        public void SetUpdateOrder(int order)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }
    }
}
