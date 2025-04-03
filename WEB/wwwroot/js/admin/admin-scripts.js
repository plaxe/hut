document.addEventListener('DOMContentLoaded', function() {
    // Sidebar toggler
    const sidebarToggler = document.getElementById('sidebarToggler');
    const desktopSidebarToggler = document.getElementById('desktopSidebarToggler');
    const adminSidebar = document.getElementById('adminSidebar');
    const adminContent = document.getElementById('adminContent');
    const loadingIndicator = document.getElementById('loadingIndicator');
    
    // Проверяем, было ли состояние боковой панели сохранено
    const sidebarState = localStorage.getItem('adminSidebarState');
    if (sidebarState === 'collapsed' && window.innerWidth >= 768) {
        adminSidebar.classList.add('collapsed');
        adminContent.classList.add('expanded');
        
        // Обновляем иконку кнопки
        if (desktopSidebarToggler) {
            desktopSidebarToggler.querySelector('i').classList.replace('bi-layout-sidebar-inset', 'bi-layout-sidebar');
        }
    }
    
    if (sidebarToggler && adminSidebar && adminContent) {
        // Mobile sidebar toggle
        sidebarToggler.addEventListener('click', function(e) {
            e.stopPropagation();
            adminSidebar.classList.toggle('mobile-show');
        });
        
        // Desktop sidebar toggle
        if (desktopSidebarToggler) {
            desktopSidebarToggler.addEventListener('click', function() {
                adminSidebar.classList.toggle('collapsed');
                adminContent.classList.toggle('expanded');
                
                // Обновляем иконку кнопки
                const icon = this.querySelector('i');
                if (adminSidebar.classList.contains('collapsed')) {
                    icon.classList.replace('bi-layout-sidebar-inset', 'bi-layout-sidebar');
                    localStorage.setItem('adminSidebarState', 'collapsed');
                } else {
                    icon.classList.replace('bi-layout-sidebar', 'bi-layout-sidebar-inset');
                    localStorage.setItem('adminSidebarState', 'expanded');
                }
            });
        }
        
        // Hide sidebar when clicking outside on mobile
        document.addEventListener('click', function(e) {
            if (window.innerWidth < 768 && 
                adminSidebar.classList.contains('mobile-show') && 
                !adminSidebar.contains(e.target) && 
                !sidebarToggler.contains(e.target)) {
                adminSidebar.classList.remove('mobile-show');
            }
        });
        
        // Responsive adjustments
        window.addEventListener('resize', function() {
            if (window.innerWidth >= 768) {
                adminSidebar.classList.remove('mobile-show');
                
                // Восстанавливаем состояние сворачивания при изменении размера окна
                const sidebarState = localStorage.getItem('adminSidebarState');
                if (sidebarState === 'collapsed') {
                    adminSidebar.classList.add('collapsed');
                    adminContent.classList.add('expanded');
                    
                    if (desktopSidebarToggler) {
                        desktopSidebarToggler.querySelector('i').classList.replace('bi-layout-sidebar-inset', 'bi-layout-sidebar');
                    }
                }
            }
        });
    }
    
    // Loading indicator
    if (loadingIndicator) {
        // Show loading indicator on navigation
        document.addEventListener('click', function(e) {
            const link = e.target.closest('a');
            if (link && !link.getAttribute('target') && link.getAttribute('href') && 
                !link.getAttribute('href').startsWith('#') && 
                !link.getAttribute('href').startsWith('javascript') && 
                !e.ctrlKey && !e.metaKey && 
                !link.hasAttribute('data-bs-toggle')) { // Исключаем ссылки с data-bs-toggle (для модальных окон)
                loadingIndicator.classList.add('active');
            }
        });
        
        // Скрываем индикатор загрузки при завершении загрузки страницы
        window.addEventListener('load', function() {
            loadingIndicator.classList.remove('active');
        });
    }
    
    // Make tables responsive on small screens
    const tables = document.querySelectorAll('table');
    tables.forEach(table => {
        if (!table.parentElement.classList.contains('table-responsive')) {
            const wrapper = document.createElement('div');
            wrapper.classList.add('table-responsive');
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
    
    // Автоматически скрываем уведомления через 5 секунд
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
}); 