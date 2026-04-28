// Global JS behavior for TechGear Shop Customer site

document.addEventListener("DOMContentLoaded", function () {
    // 1. Setup Toast notification style using SweetAlert2
    window.Toast = Swal.mixin({
        toast: true,
        position: 'bottom-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer)
            toast.addEventListener('mouseleave', Swal.resumeTimer)
        }
    });

    // Hiện Promo Popup Quảng cáo 1 lần duy nhất mỗi session
    if (!sessionStorage.getItem('promoShown')) {
        setTimeout(() => {
            const promoEl = document.getElementById('promoModal');
            if (promoEl) {
                const promoModal = new bootstrap.Modal(promoEl);
                promoModal.show();
                sessionStorage.setItem('promoShown', 'true');
            }
        }, 2000);
    }

    // 2. Intercept "Add to Cart" forms to process via AJAX
    document.querySelectorAll('.ajax-add-to-cart').forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault(); // Ngăn trình duyệt load lại trang

            const submitBtn = form.querySelector('button[type="submit"]');
            const originalHtml = submitBtn.innerHTML;

            // Effect loading
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';
            submitBtn.disabled = true;

            const url = form.action;
            const formData = new FormData(form);

            try {
                const response = await fetch(url, {
                    method: 'POST',
                    body: formData,
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                const result = await response.json();

                if (result.success) {
                    Toast.fire({ icon: 'success', title: result.message });
                    // Update header badge live
                    updateCartBadge(result.cartCount);
                } else {
                    if (result.requireLogin) {
                        Swal.fire({
                            title: 'Chưa đăng nhập!',
                            text: 'Sếp vui lòng đăng nhập để thêm đồ vào giỏ hàng nhé.',
                            icon: 'warning',
                            showCancelButton: true,
                            confirmButtonText: 'Đăng nhập ngay',
                            cancelButtonText: 'Đóng'
                        }).then((dialog) => {
                            if (dialog.isConfirmed) {
                                window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                            }
                        });
                    } else {
                        Toast.fire({ icon: 'error', title: result.message });
                    }
                }
            } catch (err) {
                console.error(err);
                Toast.fire({ icon: 'error', title: 'Đã có lỗi xảy ra. Hãy thử lại.' });
            } finally {
                // Restore button
                submitBtn.innerHTML = originalHtml;
                submitBtn.disabled = false;
            }
        });
    });

    // Hàm update icon giỏ hàng trên Header bằng DOM thao tác
    function updateCartBadge(count) {
        const cartLink = document.querySelector('a[href="/Cart"]');
        if (cartLink) {
            let badge = cartLink.querySelector('.badge');
            if (!badge && count > 0) {
                // Chưa có badge thì insert html
                cartLink.insertAdjacentHTML('beforeend', `<span class="position-absolute top-10 start-100 translate-middle badge rounded-pill bg-danger">${count}<span class="visually-hidden">sản phẩm trong giỏ</span></span>`);
            } else if (badge && count > 0) {
                badge.innerHTML = `${count} <span class="visually-hidden">sản phẩm trong giỏ</span>`;
            } else if (badge && count <= 0) {
                badge.remove();
            }
        }
    }   // end updateCartBadge
});


/**
 * Custom Language Switcher — Dùng cookie `googtrans` chuẩn của Google.
 * Đây là cách đáng tin cậy 100%: set cookie rồi reload.
 */
function switchLang(langCode, flagEmoji, label) {
    const domain = location.hostname;
    const expire = new Date(Date.now() + 365 * 24 * 3600 * 1000).toUTCString();

    if (langCode === 'vi') {
        // Xóa cookie để về ngôn ngữ gốc
        document.cookie = 'googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        document.cookie = `googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=${domain}`;
        document.cookie = `googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=.${domain}`;
    } else {
        const val = `/vi/${langCode}`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/;`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/; domain=${domain}`;
        document.cookie = `googtrans=${val}; expires=${expire}; path=/; domain=.${domain}`;
    }
    location.reload();
}

// Đọc cookie hiện tại để cập nhật UI nút cho đúng khi page load
(function syncLangButton() {
    const match = document.cookie.match(/googtrans=\/vi\/(\w+)/);
    if (match && match[1] === 'en') {
        const flagEl = document.getElementById('active-lang-flag');
        const textEl = document.getElementById('active-lang-text');
        if (flagEl) flagEl.textContent = '🇬🇧';
        if (textEl) textEl.textContent = 'EN';
    }
})();

/**
 * =========================================================
 * NOTIFICATION SYSTEM LOGIC
 * =========================================================
 */
let notificationsData = [];

async function fetchNotifications() {
    try {
        const res = await fetch('/api/Notification/GetList?limit=30');
        const result = await res.json();
        if (result.success) {
            notificationsData = result.data;
            updateNotifBadge(result.unreadCount);
            renderNotifications('all');
        }
    } catch (err) {
        console.error("Lỗi khi tải thông báo", err);
    }
}

function updateNotifBadge(count) {
    const badge = document.getElementById('notifBadge');
    if (!badge) return;
    if (count > 0) {
        badge.textContent = count > 9 ? '9+' : count;
        badge.style.display = 'inline-block';
    } else {
        badge.style.display = 'none';
    }
}

function renderNotifications(filterStr) {
    const container = document.getElementById('notifListContainer');
    if (!container) return;

    let filtered = notificationsData;
    if (filterStr === 'order') {
        filtered = notificationsData.filter(n => n.typeString === 'Order');
    } else if (filterStr === 'promo') {
        filtered = notificationsData.filter(n => n.typeString === 'Promotion');
    }

    if (filtered.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted p-5 d-flex flex-column align-items-center justify-content-center h-100 opacity-75">
                <i class="bi bi-bell-slash fs-1 mb-3"></i>
                <h6 class="fw-bold">Bạn chưa có thông báo nào</h6>
                <small>Hãy tiếp tục mua sắm để nhận thông báo nhé.</small>
            </div>
        `;
        return;
    }

    container.innerHTML = filtered.slice(0, 7).map(n => {
        const unreadClass = !n.isRead ? 'bg-primary bg-opacity-10 fw-bold border-start border-4 border-primary' : '';
        const iconClass = n.typeString === 'Order' ? 'bi-box-seam text-success' :
            n.typeString === 'Promotion' ? 'bi-tags-fill text-danger' :
                n.typeString === 'Account' ? 'bi-person-badge text-info' : 'bi-bell-fill text-warning';

        return `
            <div class="p-3 border-bottom position-relative notif-item ${unreadClass}" style="cursor: pointer; transition: all 0.2s ease;" onclick="readAndNavigate(${n.id}, '${n.linkTo || '/'}')">
                <div class="d-flex gx-3">
                    <div class="me-3">
                        <div class="rounded-circle d-flex align-items-center justify-content-center bg-white shadow-sm" style="width: 40px; height: 40px;">
                            <i class="bi ${iconClass} fs-5"></i>
                        </div>
                    </div>
                    <div>
                        <h6 class="mb-1 text-dark" style="font-size: 0.95rem;">${n.title}</h6>
                        <p class="mb-1 text-muted small lh-sm line-clamp-2">${n.message}</p>
                        <small class="text-secondary" style="font-size: 0.75rem;"><i class="bi bi-clock me-1"></i>${n.createdAt}</small>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function filterNotifs(type, btnEl) {
    document.querySelectorAll('.notification-tabs .btn').forEach(b => {
        b.classList.remove('active-tab', 'text-dark', 'fw-bold');
        b.classList.add('text-muted');
    });
    btnEl.classList.add('active-tab', 'text-dark', 'fw-bold');
    btnEl.classList.remove('text-muted');

    renderNotifications(type);
}

async function readAndNavigate(id, url) {
    await fetch(`/api/Notification/MarkAsRead/${id}`, { method: 'POST' });
    window.location.href = url;
}

async function markAllNotifAsRead() {
    const res = await fetch('/api/Notification/MarkAllAsRead', { method: 'POST' });
    if (res.ok) {
        fetchNotifications();
    }
}

// Lần đầu load trang: Fetch số lượng huy hiệu (nếu chưa gọi click)
document.addEventListener("DOMContentLoaded", function () {
    if (document.getElementById('notifBadge')) fetchNotifications();

    /**
     * =========================================================
     * REAL-TIME SIGNALR CONNECTION
     * =========================================================
     */
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.on("ReceiveNotification", function (notification) {
        // Tăng badge
        const badge = document.getElementById('notifBadge');
        if (badge) {
            let count = parseInt(badge.textContent) || 0;
            updateNotifBadge(count + 1);
        }

        // Bắn SweetAlert Toast Realtime
        if (window.Toast) {
            window.Toast.fire({
                icon: 'info',
                title: 'Ting ting! Bạn có thông báo mới',
                text: notification.title
            });
        }

        // Đẩy thông báo mới lên đầu danh sách ngầm
        notificationsData.unshift(notification);

        // Nếu cái Dropdown đang mở, render lại DOM luôn
        const dropdownParent = document.getElementById('notificationMenuContainer');
        if (dropdownParent && dropdownParent.querySelector('.dropdown-menu').classList.contains('show')) {
            // Xem người dùng đang ở Tab nào
            let activeTab = 'all';
            document.querySelectorAll('.notification-tabs .btn').forEach(btn => {
                if (btn.classList.contains('active-tab')) {
                    if (btn.textContent.includes('Đơn hàng')) activeTab = 'order';
                    else if (btn.textContent.includes('Khuyến mãi')) activeTab = 'promo';
                }
            });
            renderNotifications(activeTab);
        }
    });

    // Lắng nghe sự kiện cực đỉnh cho Tab Đơn hàng (Cập nhật riêng)
    connection.on("OrderStatusUpdated", function (data) {
        if (window.location.pathname.toLowerCase().includes('/account/orders')) {
            // Hiện Toast rực rỡ báo thay đổi trạng thái
            if (window.Toast) {
                window.Toast.fire({
                    icon: 'success',
                    title: 'Trạng thái đơn hàng #' + data.orderId + ' thay đổi',
                    text: 'Chuyển sang: ' + data.statusName
                });
            }

            // Đợi 2s cho user đọc Toast xong mới reload lại để cập nhật toàn bộ Tab & Nút bấm ngầm
            setTimeout(() => {
                window.location.reload();
            }, 2000);
        }
    });

    // Start Realtime Connection
    if (document.getElementById('notifBadge')) { // Chỉ kết nối nếu có cái chuông (đã Đăng nhập)
        connection.start().then(function () {
            console.log("🚀 Realtime Notification Hub Connected!");
        }).catch(function (err) {
            return console.error(err.toString());
        });
    }
});
