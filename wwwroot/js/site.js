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
    
    const selectedUsers = new Set();

    if (results_chatSideBar) {
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
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: `postId=${postId}`
            }).then(response => response.json()).then(data => {
                if (data.success) {
                    // Update all like buttons and counts for this post (for feed and modal)
                    document.querySelectorAll(`.like-button[data-post-id='${postId}']`).forEach(btn => {
                        const icon = btn.querySelector('i');
                        icon.classList.toggle('bi-heart', !data.liked);
                        icon.classList.toggle('bi-heart-fill', data.liked);
                        icon.classList.toggle('text-danger', data.liked);
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
            document.getElementById(`comment-display-${commentId}`).style.display = 'none';
            const editForm = document.querySelector(`.edit-comment-form[data-comment-id='${commentId}']`);
            editForm.style.display = 'block';
            editForm.querySelector('input[name="content"]').focus();
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
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token
                    }
                }).then(response => response.json()).then(data => {
                    if (data.success) {
                        document.getElementById(`comment-container-${commentId}`).remove();
                    } else {
                        alert(data.message || 'Failed to delete comment.');
                    }
                }).catch(() => alert('An error occurred while deleting the comment.'));
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
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body: `postId=${postId}&content=${encodeURIComponent(content)}`
            }).then(response => response.text()).then(html => {
                // Works for both feed and modal
                const commentsContainer = document.getElementById(`comments-for-${postId}`) || document.getElementById(`comments-for-modal-${postId}`);
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
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: `content=${encodeURIComponent(content)}`
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    document.getElementById(`comment-content-${commentId}`).textContent = data.content;
                    document.getElementById(`comment-display-${commentId}`).style.display = 'flex';
                    form.style.display = 'none';
                } else { alert(data.message || 'Failed to edit comment.'); }
            }).catch(() => alert('An error occurred while editing the comment.'));
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

        postModal.addEventListener('show.bs.modal', function (event) {
            const button = event.relatedTarget; // The thumbnail that was clicked
            const postId = button.getAttribute('data-post-id');
            const modalBody = document.getElementById('postModalBody');

            // Show a loading spinner
            modalBody.innerHTML = '<div class="d-flex justify-content-center p-5"><div class="spinner-border text-light" role="status"><span class="visually-hidden">Loading...</span></div></div>';

            fetch(`/Posts/PostPartial/${postId}`)
                .then(response => response.text())
                .then(html => { modalBody.innerHTML = html; })
                .catch(err => { modalBody.innerHTML = '<p class="text-danger text-center p-5">Failed to load post. Please try again.</p>'; });
        });
    }

    initializePostModal();

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
                    item.innerHTML = `
                        <div class="bg-purple text-white rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 40px; height: 40px; flex-shrink:0;">
                            <i class="bi bi-people-fill"></i>
                        </div>
                        <div class="overflow-hidden">
                            <div class="fw-bold text-truncate">${g.name}</div>
                            <small class="text-muted text-truncate d-block">${g.lastMessage || 'No messages yet'}</small>
                        </div>
                    `;
                    
                    // Click opens modal
                    item.addEventListener('click', (e) => {
                        e.preventDefault();
                        openChatModal(g.id, g.name);
                    });
                    
                    groupsListContainer.appendChild(item);
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

    function loadMessages(groupId) {
        fetch(`/Group/GetMessages/${groupId}`)
            .then(res => res.json())
            .then(messages => {
                chatMessagesContainer.innerHTML = '';
                if(messages.length === 0) {
                    chatMessagesContainer.innerHTML = '<div class="text-center text-muted mt-5">Start the conversation!</div>';
                    return;
                }

                messages.forEach(msg => {
                    const isMe = msg.isMe;
                    const div = document.createElement('div');
                    div.className = `d-flex mb-3 ${isMe ? 'justify-content-end' : 'justify-content-start'}`;
                    
                    const pfpHtml = isMe ? '' : `<img src="${msg.senderPfp || '/uploads/default_pfp.jpg'}" class="rounded-circle me-2 align-self-end" width="30" height="30">`;
                    
                    div.innerHTML = `
                        ${pfpHtml}
                        <div class="d-flex flex-column ${isMe ? 'align-items-end' : 'align-items-start'}" style="max-width: 75%;">
                            ${!isMe ? `<small class="text-muted mb-1" style="font-size:0.75rem;">${msg.senderName}</small>` : ''}
                            <div class="p-2 rounded-3 ${isMe ? 'bg-purple text-white' : 'bg-white border shadow-sm'}" style="word-wrap: break-word;">
                                ${msg.content}
                            </div>
                            <small class="text-muted mt-1" style="font-size: 0.7rem;">${msg.sentAt}</small>
                        </div>
                    `;
                    chatMessagesContainer.appendChild(div);
                });
                
                // Scroll to bottom
                chatMessagesContainer.scrollTop = chatMessagesContainer.scrollHeight;
            });
    }

    // 3. Send Message
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
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    chatInput.value = '';
                    loadMessages(groupId); // Reload to see new message
                }
            });
        });
    }
});