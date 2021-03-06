﻿using Samotorcan.HtmlUi.Core;

namespace Samotorcan.HtmlUi.WindowsForms
{
    /// <summary>
    /// Windows forms application context.
    /// </summary>
    public class ApplicationContext : Core.ApplicationContext
    {
        #region Properties
        #region Public

        #region WindowSettings
        /// <summary>
        /// Gets the window settings.
        /// </summary>
        /// <value>
        /// The window settings.
        /// </value>
        public WindowSettings WindowSettings { get; protected set; }
        #endregion

        #endregion
        #endregion
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Using Initialize instead of a call to base constructor.")]
        public ApplicationContext()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Using Initialize instead of a call to base constructor.")]
        public ApplicationContext(ApplicationContext settings)
        {
            Initialize(settings);
        }

        #endregion
        #region Methods
        #region Private

        #region InitializeSelf
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        private void InitializeSelf(ApplicationContext settings)
        {
            if (settings != null)
            {
                WindowSettings = settings.WindowSettings;
            }
            else
            {
                WindowSettings = new WindowSettings();
            }
        }
        #endregion

        #endregion
        #region Protected

        #region Initialize
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected virtual void Initialize(ApplicationContext settings)
        {
            base.Initialize(settings);

            InitializeSelf(settings);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void Initialize(BaseApplicationSettings settings)
        {
            base.Initialize(settings);

            InitializeSelf(null);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="settings">The settings.</param>
        protected override void Initialize(Core.ApplicationContext settings)
        {
            base.Initialize(settings);

            InitializeSelf(null);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            Initialize(null);
        }
        #endregion

        #endregion
        #endregion
    }
}
