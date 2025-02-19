import { createPopper } from '@popperjs/core';

// Global state to track the active menu
const activeMenu = {
    current: null,
    close() {
        if (this.current) {
            this.current.hide();
            this.current = null;
        }
    }
};

const contextmenu = app => {
    app.directive('contextmenu', {
        mounted(el, binding) {
            let popperInstance = null;
            const menuId = binding.value;
            const menu = document.getElementById(menuId);

            if (!menu) {
                //console.error(`Element with id '${menuId}' not found`);
                return;
            }

            menu.style.display = 'none';

            const appendToBody =
                el.getAttribute('menu-append-to-body') === 'true';

            const menuItemClass = el.getAttribute('menu-item-class') || 'a';

            const showMenu = event => {
                activeMenu.close();
                activeMenu.current = { hide: hideMenu };

                menu.classList.add('open');
                menu.style.display = 'block';

                if (appendToBody) {
                    document.body.appendChild(menu);
                    menu.style.position = 'absolute';
                }

                requestAnimationFrame(() => {
                    let mouseX = event.clientX;
                    let mouseY = event.clientY;

                    const menuWidth = menu.scrollWidth;
                    const menuHeight = menu.scrollHeight;

                    if (mouseX + menuWidth > window.innerWidth) {
                        mouseX = window.innerWidth - menuWidth;
                    }

                    if (mouseY + menuHeight > window.innerHeight) {
                        mouseY = window.innerHeight - menuHeight;
                    }

                    menu.style.top = `${mouseY}px`;
                    menu.style.left = `${mouseX}px`;

                    if (!popperInstance) {
                        popperInstance = createPopper(menu, {
                            placement: 'bottom-start'
                        });
                    }

                    popperInstance.update();

                    const menuItems =
                        menuItemClass === 'a'
                            ? menu.querySelectorAll('a')
                            : menu.querySelectorAll('.' + menuItemClass);

                    menuItems.forEach(item => {
                        item.addEventListener('click', hideMenu);
                    });
                });

                event.preventDefault();
            };

            const hideMenu = () => {
                menu.classList.remove('open');
                menu.style.display = 'none';
                if (appendToBody) {
                    menu.remove();
                }

                const menuItems =
                    menuItemClass === 'a'
                        ? menu.querySelectorAll('a')
                        : menu.querySelectorAll('.' + menuItemClass);

                menuItems.forEach(item => {
                    item.removeEventListener('click', hideMenu);
                });

                if (popperInstance) {
                    popperInstance.destroy();
                    popperInstance = null;
                }

                activeMenu.current = null;
            };

            el.addEventListener('contextmenu', showMenu);
            document.addEventListener('click', event => {
                if (!menu.contains(event.target)) {
                    hideMenu();
                }
            });

            el._contextMenu = { el, menu, popperInstance, showMenu, hideMenu };
        },
        unmounted(el) {
            if (!el._contextMenu)
                return

            let { menu, popperInstance, showMenu, hideMenu } = el._contextMenu;
            el.removeEventListener('contextmenu', showMenu);
            document.removeEventListener('click', hideMenu);

            const menuItemClass = el.getAttribute('menu-item-class') || 'a';
            const menuItems =
                menuItemClass === 'a'
                    ? menu.querySelectorAll('a')
                    : menu.querySelectorAll('.' + menuItemClass);
            menuItems.forEach(item => {
                item.removeEventListener('click', hideMenu);
            });

            if (popperInstance) {
                popperInstance.destroy();
            }
        }
    });
};

export default contextmenu;
