// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
    const input = document.querySelector('#searchInput');
    const results = document.querySelector('#searchResults');
    if (!input || !results) return;

    input.addEventListener('input', async () => {
        const q = input.value.trim();
        if (!q) { results.innerHTML = ''; results.style.display = 'none'; return; }

        try {
            const res = await fetch(`/Search/Live?q=${encodeURIComponent(q)}`);
            if (!res.ok) {
                const text = await res.text();
                console.error('Search error', res.status, text);
                results.innerHTML = '';
                results.style.display = 'none';
                return;
            }
            results.innerHTML = await res.text();
            results.style.display = 'block';
        } catch (err) {
            console.error('Search fetch failed:', err);
            results.innerHTML = '';
            results.style.display = 'none';
        }
    });

    const input_chatSideBar = document.querySelector('#searchInput_chat');
    const results_chatSideBar = document.querySelector('#searchResults_chat');
    const selectedMembersContainer = document.querySelector('#selectedMembers');
    
    // ADD NULL CHECK HERE (CRITICAL FIX)
    if (input_chatSideBar && results_chatSideBar) {
        input_chatSideBar.addEventListener('input', async () => {
            const q = input_chatSideBar.value.trim();
            if (!q) { results_chatSideBar.innerHTML = ''; results_chatSideBar.style.display = 'none'; return; }

            try {
                const res = await fetch(`/Search/LiveChat?q=${encodeURIComponent(q)}`);
                if (!res.ok) {
                    const text = await res.text();
                    console.error('Search error', res.status, text);
                    results_chatSideBar.innerHTML = '';
                    results_chatSideBar.style.display = 'none';
                    return;
                }
                results_chatSideBar.innerHTML = await res.text();
                results_chatSideBar.style.display = 'block';
            } catch (err) {
                console.error('Search fetch failed:', err);
                results_chatSideBar.innerHTML = '';
                results_chatSideBar.style.display = 'none';
            }
        });
    }
    
    const selectedUsers = new Set();

    if (results_chatSideBar && selectedMembersContainer) {
        results_chatSideBar.addEventListener('click', (e) => {
            const listItem = e.target.closest('li');
            if (!listItem) return;

            // Get data from attributes we added in the partial view
            const userId = listItem.dataset.userId;
            const username = listItem.dataset.username;
            const pfpSrc = listItem.dataset.pfp;

            if (userId && !selectedUsers.has(userId)) {
                selectedUsers.add(userId);

                // 1. Create the Visual "Chip"
                const chip = document.createElement('div');
                chip.className = 'user-chip animate-pop';
                chip.innerHTML = `
                    <img src="${pfpSrc}" alt="" />
                    <span>${username}</span>
                    <button type="button" class="btn-close btn-close-white ms-2" aria-label="Remove" style="width: 0.5em; height: 0.5em;"></button>
                    <!-- 2. Create Hidden Input for Form Submission -->
                    <input type="hidden" name="SelectedUserIds" value="${userId}" />
                `;

                selectedMembersContainer.appendChild(chip);

                // 3. Clear search to allow adding next user easily
                input_chatSideBar.value = '';
                results_chatSideBar.style.display = 'none';
                input_chatSideBar.focus();

                // 4. Remove Logic
                chip.querySelector('.btn-close').addEventListener('click', () => {
                    chip.remove(); // Removes the visual + the hidden input inside it
                    selectedUsers.delete(userId);
                });
            }
            else{
                // User already selected, just clear input and results
                input_chatSideBar.value = '';
                results_chatSideBar.style.display = 'none';
                input_chatSideBar.focus();   
            }
        });
    }

    // hide results when clicking outside
    document.addEventListener('click', (e) => {
        if (e.target !== input && !results.contains(e.target)) {
            results.style.display = 'none';
        }
    });

    // =================================================================
    // Centralized Event Handlers for Likes, Comments, etc.
    // =================================================================

    // Use event delegation on the document to handle clicks on dynamic content
    document.addEventListener('click', function (e) {
        const likeButton = e.target.closest('.like-button');
        const editButton = e.target.closest('.edit-comment-btn');
        const deleteButton = e.target.closest('.delete-comment-btn');

        // --- Handle Like Button Click ---
        if (likeButton) {
            e.preventDefault();
            const postId = likeButton.dataset.postId;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch('/Posts/ToggleLike', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `postId=${postId}&__RequestVerificationToken=${encodeURIComponent(token)}`
            }).then(response => response.json()).then(data => {
                if (data.success) {
                    document.querySelectorAll(`.like-button[data-post-id='${postId}']`).forEach(btn => {
                        const icon = btn.querySelector('i');
                        if (data.liked) {
                            icon.classList.remove('bi-heart');
                            icon.classList.add('bi-heart-fill', 'text-danger');
                        } else {
                            icon.classList.remove('bi-heart-fill', 'text-danger');
                            icon.classList.add('bi-heart');
                        }
                    });
                    document.querySelectorAll(`#post-${postId} .likes-count, #post-modal-${postId} .likes-count`).forEach(span => {
                        span.textContent = `${data.likesCount} likes`;
                    });
                }
            }).catch(error => console.error('Error toggling like:', error));
        }

        // --- Handle Edit Button Click (to show the form) ---
        if (editButton) {
            e.preventDefault();
            const commentId = editButton.dataset.commentId;
            const displayDiv = document.getElementById(`comment-display-${commentId}`);
            const editForm = document.querySelector(`.edit-comment-form[data-comment-id='${commentId}']`);
            
            if (displayDiv && editForm) {
                displayDiv.style.display = 'none';
                editForm.style.display = 'block';
                editForm.querySelector('input[name="content"]').focus();
            }
        }

        // --- Handle Delete Button Click ---
        if (deleteButton) {
            e.preventDefault();
            const commentId = deleteButton.dataset.commentId;
            
            if (confirm('Are you sure you want to delete this comment?')) {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                
                fetch(`/Comments/Delete/${commentId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: `__RequestVerificationToken=${encodeURIComponent(token)}`
                })
                .then(async response => { // Make this async to await the body text
                    if (!response.ok) {
                        // Try to get a JSON error message from the body
                        const errorData = await response.json().catch(() => null);
                        const errorMessage = errorData?.message || `Network response was not ok (Status: ${response.status})`;
                        throw new Error(errorMessage);
                    }
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        const container = document.getElementById(`comment-container-${commentId}`);
                        if (container) {
                            container.style.transition = 'opacity 0.3s';
                            container.style.opacity = '0';
                            setTimeout(() => container.remove(), 300);
                        }
                    } else {
                        alert(data.message || 'Failed to delete comment.');
                    }
                }).catch(err => {
                    console.error('Delete error:', err);
                    alert(err.message); // Display the more specific error message
                });
            }
        }
    });

    // Use event delegation for form submissions
    document.addEventListener('submit', function (e) {
        // --- Handle Add Comment Form Submission ---
        if (e.target.matches('.add-comment-form')) {
            e.preventDefault();
            const form = e.target;
            const postId = form.dataset.postId;
            const input = form.querySelector('input[name="commentText"]');
            const content = input.value;

            if (!content.trim()) return;

            fetch('/Comments/Add', {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/x-www-form-urlencoded', 
                    'X-Requested-With': 'XMLHttpRequest' 
                },
                body: `postId=${postId}&content=${encodeURIComponent(content)}`
            }).then(response => response.text()).then(html => {
                const commentsContainer = 
                    document.getElementById(`comments-list-${postId}`) ||
                    document.getElementById(`comments-for-modal-${postId}`);
                
                if (commentsContainer) {
                    commentsContainer.insertAdjacentHTML('beforeend', html);
                }
                input.value = '';
            }).catch(error => console.error('Error adding comment:', error));
        }

        // --- Handle Edit Comment Form Submission ---
        if (e.target.matches('.edit-comment-form')) {
            e.preventDefault();
            const form = e.target;
            const commentId = form.dataset.commentId;
            const content = form.querySelector('input[name="content"]').value;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            fetch(`/Comments/Edit/${commentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: `content=${encodeURIComponent(content)}&__RequestVerificationToken=${encodeURIComponent(token)}`
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    const contentSpan = document.getElementById(`comment-content-${commentId}`);
                    const displayDiv = document.getElementById(`comment-display-${commentId}`);
                    
                    if (contentSpan) contentSpan.textContent = data.content;
                    if (displayDiv) displayDiv.style.display = 'flex';
                    form.style.display = 'none';
                } else { 
                    alert(data.message || 'Failed to edit comment.'); 
                }
            }).catch(err => {
                console.error('Edit error:', err);
                alert('An error occurred while editing the comment.');
            });
        }
    });

    // =================================================================
    // Infinite Scroll for Home Feed
    // =================================================================
    function initializeInfiniteScroll() {
        const mainContent = document.querySelector('.main-content');
        const feedContainer = document.getElementById('feed-container');
        const loadingIndicator = document.getElementById('loading');

        // Exit if the necessary elements aren't on this page
        if (!mainContent || !feedContainer || !loadingIndicator) {
            return;
        }

        let page = 1;
        let isLoading = false;
        let noMorePosts = false;

        mainContent.onscroll = () => {
            if (isLoading || noMorePosts) return;

            // Check if user is near the bottom (added a slightly larger threshold)
            if (mainContent.scrollHeight - mainContent.scrollTop <= mainContent.clientHeight + 200) {
                isLoading = true;
                loadingIndicator.innerHTML = '<div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div>';
                loadingIndicator.style.display = 'block';

                fetch(`/Home/LoadMorePosts?page=${page}`)
                    .then(response => response.text())
                    .then(data => {
                        if (data && data.trim().length > 0) {
                            feedContainer.insertAdjacentHTML('beforeend', data);
                            page++;
                            loadingIndicator.style.display = 'none';
                            loadingIndicator.innerHTML = '';
                        } else {
                            // No more posts to load
                            noMorePosts = true;
                            loadingIndicator.innerHTML = '<p class="text-muted small mt-4">You have reached the end.</p>';
                            // Keep the message displayed
                        }
                        isLoading = false;
                    }).catch(() => { isLoading = false; loadingIndicator.style.display = 'none'; loadingIndicator.innerHTML = ''; });
            }
        };
    }

    initializeInfiniteScroll();

    // =================================================================
    // Post Modal Loading for Profile Page
    // =================================================================
    function initializePostModal() {
        const postModal = document.getElementById('postModal');
        if (!postModal) {
            return; // Exit if the modal isn't on this page
        }

        // Remove any existing event listeners to prevent duplicates
        postModal.removeEventListener('show.bs.modal', handleModalShow);
        postModal.addEventListener('show.bs.modal', handleModalShow);
    }

    function handleModalShow(event) {
        const button = event.relatedTarget; // The thumbnail that was clicked
        if (!button) {
            console.error('No button trigger found');
            return;
        }
        
        const postId = button.getAttribute('data-post-id');
        if (!postId) {
            console.error('No post ID found on button:', button);
            return;
        }
        
        const modalBody = document.getElementById('postModalBody');
        if (!modalBody) {
            console.error('Modal body not found');
            return;
        }

        // Show a loading spinner
        modalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-purple" role="status"><span class="visually-hidden">Loading...</span></div></div>';

        fetch(`/Posts/PostPartial/${postId}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.text();
            })
            .then(html => { 
                modalBody.innerHTML = html;
            })
            .catch(err => { 
                console.error('Modal load error:', err);
                modalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post. Please try again.</p>'; 
            });
    }

    // Initialize modal after a short delay to ensure Bootstrap is ready
    setTimeout(() => {
        initializePostModal();
    }, 100);

    // =================================================================
    // SIMPLE SIDEBAR TOGGLE LOGIC
    // =================================================================
    function toggleSidebar(side) {
        const layout = document.querySelector('.home-layout');
        if (!layout) return;

        if (side === 'left') {
            layout.classList.toggle('left-collapsed');
        } else if (side === 'right') {
            layout.classList.toggle('right-collapsed');
        }
    }

    // Ensure the function is available globally
    window.toggleSidebar = toggleSidebar;

    // =================================================================
    // GROUP CHAT LOGIC
    // =================================================================
    
    const groupsListContainer = document.getElementById('userGroupsList');
    const chatModal = document.getElementById('groupChatModal');
    const chatMessagesContainer = document.getElementById('chatMessagesContainer');
    const chatForm = document.getElementById('chatForm');
    const chatInput = document.getElementById('chatInput');
    const currentGroupIdInput = document.getElementById('currentGroupId');
    const chatModalTitle = document.getElementById('chatModalTitle');

    // 1. Load Groups on Page Load
    if (groupsListContainer) {
        fetch('/Group/GetUserGroups')
            .then(res => res.json())
            .then(groups => {
                groupsListContainer.innerHTML = '';
                if (groups.length === 0) {
                    groupsListContainer.innerHTML = `
                        <div class="p-3 text-center text-muted bg-light rounded">
                            <i class="bi bi-chat-dots fs-4 d-block mb-2"></i>
                            <small>No active conversations</small>
                        </div>`;
                    return;
                }

                groups.forEach(g => {
                    const item = document.createElement('a');
                    item.href = '#';
                    item.className = 'list-group-item list-group-item-action border-0 rounded mb-1 d-flex align-items-center p-2';

                    const isDirectMessage = g.isDm || g.IsDm || g.isDirectMessage;
                    let iconHtml = '';

                    if (isDirectMessage) {
                        const pfpUrl = g.pfp || '/uploads/default_pfp.jpg';
                        iconHtml = `
                          <div class="chat-avatar-outline me-3">
                            <img src="${pfpUrl}" alt="">
                          </div>`;
                    } else {
                        iconHtml = `
                          <div class="chat-avatar-outline me-3">
                            <div class="chat-icon">
                              <i class="bi bi-people-fill"></i>
                            </div>
                          </div>`;
                    }

                    item.innerHTML = `
                        ${iconHtml}
                        <div class="overflow-hidden">
                            <div class="fw-bold text-truncate">${g.name}</div>
                            <small class="text-muted text-truncate d-block">${g.lastMessage || 'No messages yet'}</small>
                        </div>
                    `;
                    item.addEventListener('click', (e) => {
                        e.preventDefault();
                        openChatModal(g.id, g.name);
                    });
                    groupsListContainer.appendChild(item);
                });

    // Follow button flash effect
    document.querySelectorAll('.btn-follow-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.classList.add('flash-pulse');
            // Let animation start before form submit (optional slight delay)
            setTimeout(() => {
                btn.closest('form').submit();
            }, 120);
        });
    });

            })
            .catch(err => console.error('Failed to load groups', err));
    }

    // 2. Open Chat Modal & Load Messages
    function openChatModal(groupId, groupName) {
        currentGroupIdInput.value = groupId;
        chatModalTitle.textContent = groupName;
        chatMessagesContainer.innerHTML = '<div class="text-center mt-4"><div class="spinner-border text-purple" role="status"></div></div>';
        
        const bsModal = new bootstrap.Modal(chatModal);
        bsModal.show();

        loadMessages(groupId);
    }

   // ...existing code...

    function scrollChatToBottom(retries = 3) {
        if (!chatMessagesContainer) return;
        requestAnimationFrame(() => {
            chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
            // Fallback: use last message
            const last = chatMessagesContainer.lastElementChild;
            if (last) last.scrollIntoView({ block: 'end' });
        });
        if (retries > 0) {
            setTimeout(() => scrollChatToBottom(retries - 1), 60); // retry after images/layout settle
        }
    }

    function loadMessages(groupId) {
        fetch(`/Group/GetMessages/${groupId}`)
            .then(res => res.json())
            .then(data => {
                chatMessagesContainer.innerHTML = '';

                // FIXED: Update header icon based on conversation type
                const headerIconContainer = chatModal.querySelector('.modal-header .rounded-circle');
                if (headerIconContainer) {
                    if (data.isDm && data.headerPfp) {
                        // For 1:1 DMs, show the other user's profile picture
                        headerIconContainer.innerHTML = ''; // Clear the default icon
                        headerIconContainer.style.backgroundImage = `url('${data.headerPfp}')`;
                        headerIconContainer.style.backgroundSize = 'cover';
                        headerIconContainer.style.backgroundPosition = 'center';
                    } else {
                        // For group chats, show the default group icon
                        headerIconContainer.innerHTML = '<i class="bi bi-people-fill" style="color: #6f42c1;"></i>';
                        headerIconContainer.style.backgroundImage = 'none';
                    }
                }
                
                if (data.messages.length === 0) {
                    chatMessagesContainer.innerHTML = '<div class="text-center text-muted mt-5">Start the conversation!</div>';
                    return;
                }

                data.messages.forEach(msg => {
                    const isMe = msg.isMe;
                    const div = document.createElement('div');
                    div.className = `d-flex mb-3 ${isMe ? 'justify-content-end' : 'justify-content-start'}`;

                    const pfpHtml = isMe ? '' : `
                        <a href="/Profile/Show/${encodeURIComponent(msg.senderName)}" class="text-decoration-none">
                            <img src="${msg.senderPfp || '/uploads/default_pfp.jpg'}"
                                 class="rounded-circle me-2 align-self-end"
                                 width="30" height="30"
                                 style="object-fit:cover;">
                        </a>`;

                    div.innerHTML = `
                        ${pfpHtml}
                        <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width:70%;">
                            ${!isMe ? `<small class="text-muted mb-1 fw-semibold" style="font-size:0.75rem;">${msg.senderName}</small>` : ''}
                            <div class="p-3 rounded-3 ${isMe ? 'msg-bubble-sent' : 'msg-bubble-received'}" style="word-wrap:break-word;box-shadow:0 1px 2px rgba(0,0,0,0.1);">
                                ${msg.content}
                            </div>
                            <small class="text-muted mt-1" style="font-size:0.7rem;">${msg.sentAt}</small>
                        </div>
                    `;
                    chatMessagesContainer.appendChild(div);
                });

                // Scroll to bottom after rendering
                scrollChatToBottom();
            });
    }

    if (chatForm) {
        chatForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const content = chatInput.value.trim();
            const groupId = currentGroupIdInput.value;
            if (!content || !groupId) return;

            fetch('/Group/SendMessage', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `groupId=${groupId}&content=${encodeURIComponent(content)}`
            })
            .then(r => r.json())
            .then(d => {
                if (d.success) {
                    chatInput.value = '';
                    loadMessages(groupId);
                    // Extra final pass
                    setTimeout(() => scrollChatToBottom(), 120);
                }
            });
        });
    }

    // =================================================================
    // Notifications
    // =================================================================
    const notifBadge = document.getElementById('notifBadge');
    const notifList = document.getElementById('notifList');
    const notifMarkAll = document.getElementById('notifMarkAll');

    async function fetchNotifications() {
        if (!notifList) return;
        try {
            const res = await fetch('/Notifications/Unread');
            if (!res.ok) return;
            const data = await res.json();
            notifList.innerHTML = '';
            if (data.length === 0) {
                notifList.innerHTML = '<div class="p-3 text-muted small text-center">No new notifications</div>';
                notifBadge.classList.add('d-none');
                return;
            }
            notifBadge.textContent = data.length > 9 ? '9+' : data.length;
            notifBadge.classList.remove('d-none');

            data.forEach(n => {
                let text = '';
                switch (n.type) {
                    case 'Like': text = `<strong>${n.actor}</strong> liked your post.`; break;
                    case 'Comment': text = `<strong>${n.actor}</strong> commented on your post.`; break;
                    case 'Follow': text = `<strong>${n.actor}</strong> started following you.`; break;
                    case 'FollowRequest': text = `<strong>${n.actor}</strong> requested to follow you.`; break;
                    default: text = 'New notification';
                }
                const item = document.createElement('a');
                // Link to post, or profile for follows
                item.href = n.postId ? `/Posts/Post/${n.postId}` : (n.type === 'Follow' || n.type === 'FollowRequest' ? `/Profile/Show/${n.actor}` : '#');
                item.className = 'list-group-item list-group-item-action d-flex align-items-center gap-3';
                
                item.innerHTML = `
                    <img src="${n.actorPfp || '/uploads/default_pfp.jpg'}" class="rounded-circle" width="40" height="40" style="object-fit:cover;">
                    <div class="flex-grow-1">
                        <div class="small notification-text">${text}</div>
                        <div class="text-muted" style="font-size: 0.75rem;">${new Date(n.time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
                    </div>
                    <button class="btn btn-sm btn-link text-muted p-0 notif-read-btn" data-id="${n.id}" title="Mark as read">
                        <i class="bi bi-check-circle"></i>
                    </button>
                `;
                notifList.appendChild(item);
            });
        } catch (err) {
            console.error('Notification fetch error:', err);
        }
    }

    document.addEventListener('click', e => {
        const btn = e.target.closest('.notif-read-btn');
        if (btn) {
            e.preventDefault();
            const id = btn.dataset.id;
            fetch('/Notifications/MarkRead', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: `id=${id}`
            }).then(r => r.json()).then(d => {
                if (d.success) fetchNotifications();
            });
        }
    });

    if (notifMarkAll) {
        notifMarkAll.addEventListener('click', () => {
            fetch('/Notifications/MarkAll', { method: 'POST' })
                .then(r => r.json())
                .then(d => { if (d.success) fetchNotifications(); });
        });
    }

    fetchNotifications();
    setInterval(fetchNotifications, 30000);
});